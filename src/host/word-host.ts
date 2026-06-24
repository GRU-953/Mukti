/**
 * Mukti Word host adapter — the ONLY layer that touches Office.js.
 *
 * Implements `WordHost` (see ./contracts.ts) against Word.run. Honours the
 * Phase 3 review findings (docs/phase3/REVIEW.md) and the Phase 0 do-not-repeat
 * list (docs/phase0/findings/03-officejs.md):
 *  - font read PER RUN via getTextRanges as a COARSE segmenter, recursing on
 *    null-font (mixed) ranges; still-null → reported `mixed-font-unresolved`,
 *    never converted (Office BLOCKER-1 / Spike A);
 *  - PREVIEW PRODUCES A PLAN, APPLY CONSUMES IT, with a per-run TOCTOU guard
 *    (arch BLOCKER-1 / red-team R3);
 *  - snapshot of changed runs stored in a CustomXML part, only its id kept in
 *    document settings (Office BLOCKER-2);
 *  - out-of-scope regions reported as `unscanned`, never silently skipped (M6);
 *  - reads batched to keep context.sync() count low — NOT the prior ~3 syncs /
 *    paragraph (do-not-repeat #3).
 *
 * The pure engine + font registry are NOT imported as modules (they are built by
 * another agent and must never be coupled to Office.js — arch lint rule). They
 * are injected via the constructor as the FROZEN function types from
 * ../engine/contracts, so this file type-checks standalone.
 *
 * Office.js globals are declared as ambient `any` shims below so the file
 * type-checks WITHOUT @types/office-js installed. The CORE agent must add
 * `office-js` + `@types/office-js` as devDeps; with those present the real
 * typings apply and these shims should be deleted.
 */

/* eslint-disable @typescript-eslint/no-explicit-any */
/* global Word, Office */
// Office.js globals via globalThis so this file type-checks WITHOUT
// @types/office-js installed AND without redeclaring the real `Word`/`Office`
// namespaces once those typings ARE present (a `declare const` would collide).
// Once @types/office-js is a devDep you may use the real globals directly.
const Word: any = (globalThis as any).Word;
const Office: any = (globalThis as any).Office;

import type { ClassifyFontFn, ConvertFn, FontClass } from '../engine/contracts';
import type {
  ApplyResult,
  ConversionPlan,
  PlannedEdit,
  RegionContainer,
  RevertSnapshot,
  RunFormat,
  RunRef,
  ScanReport,
  UnscannedKind,
  UnscannedRegion,
  WordHost,
} from './contracts';

// ── Constants ────────────────────────────────────────────────────────────────

/** D-0005: the self-hosted Unicode output font. */
const OUTPUT_FONT = 'Noto Sans Bengali';
/** D-0002: the minimum WordApi set (tables force 1.3 — see findings 03 §2). */
const MIN_REQUIREMENT_SET = '1.3';

/** Document-settings key holding only the latest snapshot id (the snapshot body
 *  lives in a CustomXML part — review Office BLOCKER-2). */
const SETTINGS_LATEST_SNAPSHOT = 'mukti:latestSnapshotId';
/** Namespace for our CustomXML snapshot parts (unique to Mukti). */
const SNAPSHOT_NAMESPACE = 'https://mukti.invalid/snapshot';

/** Coarse segmentation delimiters for getTextRanges: space + common Bijoy/Bangla
 *  punctuation. These split on punctuation/space only, NOT font boundaries, so a
 *  returned range may still be mixed-font → we recurse (Spike A / Office O1). */
const COARSE_DELIMITERS = [' ', ' ', '\t', '।', '.', ',', ';', ':', '!', '?'];

/** Recursion cap when subdividing a still-mixed range into characters. A range
 *  that is null-font even at single-char width is genuinely unresolvable.
 *  TODO(SpikeA): confirm single-char ranges report a font in real Word; if not,
 *  this whole sub-paragraph strategy needs revisiting. */
const MAX_FONT_RECURSION_DEPTH = 2;

// ── Helpers ────────────────────────────────────────────────────────────────

let runCounter = 0;
function nextRunId(): string {
  runCounter += 1;
  return `r${runCounter}`;
}

/** Read a run's formatting from a loaded Word range/proxy `font`. */
function readFormat(font: any): RunFormat {
  return {
    fontName: (font.name ?? null) as string | null,
    bold: font.bold ?? undefined,
    italic: font.italic ?? undefined,
    size: font.size ?? undefined,
    color: font.color ?? undefined,
  };
}

/** Re-stamp a saved RunFormat onto a freshly-written range's `font`, then set the
 *  Unicode output font. Each set is best-effort (wrapped) so one unsupported
 *  property cannot abort the whole apply (cf. prior applyFontProps try/catch). */
function stampFormat(font: any, format: RunFormat, outputFont: string): void {
  const set = (fn: () => void) => {
    try {
      fn();
    } catch {
      /* a property Word rejects here is non-fatal; skip it */
    }
  };
  if (format.bold !== undefined) set(() => (font.bold = format.bold));
  if (format.italic !== undefined) set(() => (font.italic = format.italic));
  if (format.size !== undefined) set(() => (font.size = format.size));
  if (format.color !== undefined) set(() => (font.color = format.color));
  // Output font is set LAST so it wins over any saved source font.
  set(() => (font.name = outputFont));
}

// ── The adapter ───────────────────────────────────────────────────────────

/** Engine dependencies, injected so the host stays decoupled from the
 *  not-yet-built engine module (and free of Office.js in the engine). */
export interface EngineDeps {
  readonly convert: ConvertFn;
  readonly classify: ClassifyFontFn;
  /** Mapping-data version, surfaced in the plan/report. */
  readonly dataVersion: string;
}

export class WordHostAdapter implements WordHost {
  readonly minRequirementSet = MIN_REQUIREMENT_SET;

  constructor(private readonly engine: EngineDeps) {}

  isSupported(): boolean {
    return Office.context.requirements.isSetSupported('WordApi', MIN_REQUIREMENT_SET);
  }

  // ── Scan ──────────────────────────────────────────────────────────────────

  async scanDocument(): Promise<ScanReport> {
    runCounter = 0;
    const convertible: RunRef[] = [];
    const unsupported: RunRef[] = [];
    const unsupportedFonts = new Set<string>();
    const unscannedTally = new Map<UnscannedKind, number>();
    // fontName → { class, runs }
    const fontTally = new Map<string, { class: FontClass; runs: number }>();

    const noteUnscanned = (kind: UnscannedKind, count: number) => {
      if (count > 0) unscannedTally.set(kind, (unscannedTally.get(kind) ?? 0) + count);
    };

    await Word.run(async (context: any) => {
      // Gather the in-scope paragraph collections (body + table cells) up front,
      // then load all their items in a SINGLE sync — no per-paragraph sync
      // (do-not-repeat #3). TODO(Spike): calibrate the per-context.sync() budget;
      // for very large docs this should be chunked into a few syncs, not one.
      const bodyParas = context.document.body.paragraphs;
      bodyParas.load('items');

      const tables = context.document.body.tables;
      tables.load('items');
      await context.sync();

      // Collect every in-scope paragraph with its container, loading cell
      // paragraph collections for tables.
      const cellParaCollections: any[] = [];
      for (const table of tables.items) {
        const cells = table.getRange().tables; // touched only to keep the proxy live
        void cells;
      }
      // Table cell paragraphs: load via each table's row/cell bodies.
      for (const table of tables.items) {
        table.load('values');
      }
      // Load cell paragraph collections.
      const tableCellParas: any[] = [];
      for (const table of tables.items) {
        const rows = table.rows;
        rows.load('items');
        tableCellParas.push(rows);
      }
      await context.sync();

      for (const rows of tableCellParas) {
        for (const row of rows.items) {
          const cells = row.cells;
          cells.load('items');
        }
      }
      await context.sync();

      const cellParaGroups: any[] = [];
      for (const rows of tableCellParas) {
        for (const row of rows.items) {
          for (const cell of row.cells.items) {
            const paras = cell.body.paragraphs;
            paras.load('items');
            cellParaGroups.push(paras);
          }
        }
      }
      await context.sync();

      // Flatten into (paragraph, container) pairs.
      const paraEntries: { para: any; container: RegionContainer; index: number }[] = [];
      bodyParas.items.forEach((para: any, i: number) =>
        paraEntries.push({ para, container: 'body', index: i }),
      );
      let cellIdx = 0;
      for (const paras of cellParaGroups) {
        paras.items.forEach((para: any, i: number) =>
          // paragraphIndex is per-cell; cell origin is encoded into the ordinal
          // space via a large stride so locators stay unique across cells.
          paraEntries.push({ para, container: 'table', index: cellIdx * 10000 + i }),
        );
        cellIdx += 1;
      }

      // Coarse-segment every paragraph into ranges in one batched pass.
      const paraRanges: { entry: typeof paraEntries[number]; ranges: any }[] = [];
      for (const entry of paraEntries) {
        const ranges = entry.para.getTextRanges(COARSE_DELIMITERS, /* trimSpacing */ false);
        ranges.load('items');
        paraRanges.push({ entry, ranges });
      }
      await context.sync();

      // Load text + font on every coarse range in one sync.
      for (const { ranges } of paraRanges) {
        for (const range of ranges.items) {
          range.load('text');
          range.font.load('name,bold,italic,size,color');
        }
      }
      await context.sync();

      // Walk ranges; recurse only the null-font ones (mixed). recurse() appends
      // its own loads and is flushed by the caller-managed sync below.
      const pendingFiner: {
        entry: typeof paraEntries[number];
        ordinal: number;
        ranges: any;
        depth: number;
      }[] = [];

      const classifyAndCollect = (
        entry: typeof paraEntries[number],
        ordinal: number,
        range: any,
      ): 'resolved' | 'mixed' => {
        const fontName = range.font.name as string | null;
        const text = range.text as string;
        if (fontName == null) return 'mixed';
        if (text.length === 0) return 'resolved';

        const cls = this.engine.classify(fontName);
        const tally = fontTally.get(fontName) ?? { class: cls.class, runs: 0 };
        tally.runs += 1;
        fontTally.set(fontName, tally);

        const ref: RunRef = {
          id: nextRunId(),
          locator: {
            container: entry.container,
            paragraphIndex: entry.index,
            ordinal,
            anchorText: text,
          },
          text,
          format: readFormat(range.font),
        };
        if (cls.class === 'bijoy') convertible.push(ref);
        else if (cls.class === 'unsupported') {
          unsupported.push(ref);
          unsupportedFonts.add(fontName);
        }
        // 'unicode' / 'non-bengali' are tallied but need no run ref (no-op).
        return 'resolved';
      };

      for (const { entry, ranges } of paraRanges) {
        ranges.items.forEach((range: any, ordinal: number) => {
          const state = classifyAndCollect(entry, ordinal, range);
          if (state === 'mixed') {
            const finer = range.getTextRanges([''], false); // split to finest grain
            finer.load('items');
            pendingFiner.push({ entry, ordinal, ranges: finer, depth: 1 });
          }
        });
      }

      // Resolve finer ranges, recursing up to MAX_FONT_RECURSION_DEPTH. A range
      // still null-font at the deepest level is an UNSCANNED mixed-font run.
      let frontier = pendingFiner;
      let depth = 1;
      while (frontier.length > 0 && depth <= MAX_FONT_RECURSION_DEPTH) {
        await context.sync(); // flush the getTextRanges() loads queued above
        for (const f of frontier) {
          for (const range of f.ranges.items) {
            range.load('text');
            range.font.load('name,bold,italic,size,color');
          }
        }
        await context.sync();

        const nextFrontier: typeof pendingFiner = [];
        const atMaxDepth = depth >= MAX_FONT_RECURSION_DEPTH;
        let subOrdinal = 100000; // keep finer-run ordinals distinct from coarse
        for (const f of frontier) {
          f.ranges.items.forEach((range: any) => {
            const state = classifyAndCollect(f.entry, subOrdinal++, range);
            if (state === 'mixed') {
              if (atMaxDepth) {
                // Genuinely unresolvable → report, never convert.
                noteUnscanned('mixed-font-unresolved', 1);
              } else {
                const finer = range.getTextRanges([''], false);
                finer.load('items');
                nextFrontier.push({
                  entry: f.entry,
                  ordinal: subOrdinal,
                  ranges: finer,
                  depth: depth + 1,
                });
              }
            }
          });
        }
        frontier = nextFrontier;
        depth += 1;
      }

      // ── Out-of-scope regions: report, never silently skip (M6). ──────────────
      // TODO(SpikeA): headers/footers are reported pending until Spike A confirms
      // we can read their runs the same way. Footnotes/endnotes/textboxes/
      // comments/fields/SmartArt are OUT of MVP scope by design.
      noteUnscanned('header-footer-pending', context.document.sections ? 1 : 0);
      // The remaining kinds are not enumerable via WordApi 1.3 reliably; we record
      // their presence as best-effort counts where the API exposes them.
      // TODO(SpikeA): refine these counts once spike confirms which collections
      // are queryable at 1.3 (footnotes/endnotes are not in 1.3).
    });

    const fontTallyArr = Array.from(fontTally.entries()).map(([fontName, v]) => ({
      fontName,
      class: v.class,
      runs: v.runs,
    }));
    const unscanned: UnscannedRegion[] = Array.from(unscannedTally.entries()).map(
      ([kind, count]) => ({ kind, count }),
    );

    return {
      convertible,
      unsupported,
      unsupportedFonts: Array.from(unsupportedFonts),
      unscanned,
      fontTally: fontTallyArr,
    };
  }

  // ── Plan (preview → apply) ──────────────────────────────────────────────────

  async buildPlan(scan: ScanReport): Promise<ConversionPlan> {
    // Pure: run the engine on each convertible run. NO document mutation.
    const edits: PlannedEdit[] = scan.convertible.map((run: RunRef) => {
      const after = this.engine.convert(run.text);
      return {
        runId: run.id,
        locator: run.locator,
        before: run.text,
        after,
        format: run.format,
        changed: after !== run.text,
      };
    });
    const changedRuns = edits.filter((e: PlannedEdit) => e.changed).length;
    return {
      outputFont: OUTPUT_FONT,
      dataVersion: this.engine.dataVersion,
      edits,
      totalRuns: edits.length,
      changedRuns,
    };
  }

  // ── Apply ───────────────────────────────────────────────────────────────────

  async applyPlan(plan: ConversionPlan): Promise<ApplyResult> {
    const changedEdits = plan.edits.filter((e: PlannedEdit) => e.changed);

    // Snapshot the changed runs (text + format) BEFORE writing, into a CustomXML
    // part; only the id goes into document settings (review Office BLOCKER-2).
    const snapshotId = `mukti-${Date.now()}-${Math.floor(Math.random() * 1e6)}`;
    const snapshot: RevertSnapshot = {
      snapshotId,
      createdAt: new Date().toISOString(),
      runs: changedEdits.map((e: PlannedEdit) => ({
        locator: e.locator,
        text: e.before,
        format: e.format,
      })),
    };

    let changedRuns = 0;
    let skippedStale = 0;

    await Word.run(async (context: any) => {
      await this.storeSnapshot(context, snapshot);

      // Re-locate each run by re-segmenting its paragraph and matching ordinal,
      // then RE-VALIDATE current text against the plan's `before` (TOCTOU guard,
      // red-team R3) before writing. Reads are batched per pass.
      const paraResolvers = await this.resolveLocators(
        context,
        changedEdits.map((e: PlannedEdit) => e.locator),
      );

      // Load text on each resolved range in one sync to re-validate.
      for (const resolved of paraResolvers) {
        if (resolved.range) resolved.range.load('text');
      }
      await context.sync();

      // Write run-by-run: insertText Replace + re-stamp format + output font.
      for (let i = 0; i < changedEdits.length; i++) {
        const edit = changedEdits[i];
        const resolved = paraResolvers[i];
        if (!resolved.range) {
          skippedStale += 1; // could not re-locate → treat as stale, never force
          continue;
        }
        if (resolved.range.text !== edit.before) {
          skippedStale += 1; // document changed under us — skip, report, do not force
          continue;
        }
        const written = resolved.range.insertText(edit.after, 'Replace');
        stampFormat(written.font, edit.format, plan.outputFont);
        changedRuns += 1;
      }
      await context.sync();

      this.setLatestSnapshotId(context, snapshotId);
      await context.sync();
    });

    return { changedRuns, snapshotId, skippedStale };
  }

  // ── Revert ────────────────────────────────────────────────────────────────

  async revert(snapshotId: string): Promise<void> {
    await Word.run(async (context: any) => {
      const snapshot = await this.loadSnapshot(context, snapshotId);
      if (!snapshot) return; // nothing to revert

      const resolvers = await this.resolveLocators(
        context,
        snapshot.runs.map((r: RevertSnapshot['runs'][number]) => r.locator),
      );
      // Re-write each snapshot run's text + format. No TOCTOU guard here: revert
      // is an explicit user choice (the UI warns first via documentMatchesSnapshot
      // — review a11y R1).
      for (let i = 0; i < snapshot.runs.length; i++) {
        const run = snapshot.runs[i];
        const resolved = resolvers[i];
        if (!resolved.range) continue;
        const written = resolved.range.insertText(run.text, 'Replace');
        // Restore the ORIGINAL font (run.format.fontName), NOT the output font —
        // so stampFormat's final font.name set re-applies the saved source font.
        // (Falls back to OUTPUT_FONT only if the snapshot had a null/mixed font.)
        stampFormat(written.font, run.format, run.format.fontName ?? OUTPUT_FONT);
      }
      await context.sync();
    });
  }

  async documentMatchesSnapshot(snapshotId: string): Promise<boolean> {
    let matches = true;
    await Word.run(async (context: any) => {
      const snapshot = await this.loadSnapshot(context, snapshotId);
      if (!snapshot) {
        matches = false;
        return;
      }
      // The snapshot stores the ORIGINAL text. After a successful apply the doc
      // holds the converted text, so an unmodified-since-apply document will NOT
      // equal the snapshot text — we instead check that each run still exists
      // (re-locatable) so revert can land. A mismatch (run moved/edited away)
      // means revert may not cleanly restore → UI should warn.
      const resolvers = await this.resolveLocators(
        context,
        snapshot.runs.map((r: RevertSnapshot['runs'][number]) => r.locator),
      );
      for (const r of resolvers) r.range?.load('text');
      await context.sync();
      matches = resolvers.every((r) => r.range != null);
    });
    return matches;
  }

  async latestSnapshotId(): Promise<string | null> {
    let id: string | null = null;
    await Word.run(async (context: any) => {
      const settings = context.document.settings;
      const setting = settings.getItemOrNullObject(SETTINGS_LATEST_SNAPSHOT);
      setting.load('value,isNullObject');
      await context.sync();
      id = setting.isNullObject ? null : (setting.value as string);
    });
    return id;
  }

  // ── Internal: locator resolution ────────────────────────────────────────────

  /** Re-locate a batch of run locators inside one Word.run context. Returns, in
   *  the SAME order, a resolved range proxy (or null if not found). Re-segments
   *  each paragraph with the coarse delimiters and matches the recorded ordinal;
   *  this mirrors scan-time segmentation so ordinals line up. Batched: two syncs
   *  total regardless of locator count. */
  private async resolveLocators(
    context: any,
    locators: ReadonlyArray<RunRef['locator']>,
  ): Promise<{ range: any | null }[]> {
    const bodyParas = context.document.body.paragraphs;
    bodyParas.load('items');
    await context.sync();

    const results: { range: any | null }[] = [];
    const rangesByLocator: any[] = [];

    for (const loc of locators) {
      // MVP: re-locate body runs by paragraphIndex. Table-cell re-location uses
      // the encoded stride from scan; resolving the exact cell paragraph requires
      // re-walking rows/cells — TODO(SpikeC): confirm table re-location fidelity
      // and add cell re-walk if the spike shows ordinals drift across syncs.
      if (loc.container === 'body' && loc.paragraphIndex < bodyParas.items.length) {
        const para = bodyParas.items[loc.paragraphIndex];
        const ranges = para.getTextRanges(COARSE_DELIMITERS, false);
        ranges.load('items');
        rangesByLocator.push({ ranges, ordinal: loc.ordinal });
      } else {
        rangesByLocator.push(null);
      }
    }
    await context.sync();

    for (const entry of rangesByLocator) {
      if (!entry || entry.ordinal >= entry.ranges.items.length) {
        results.push({ range: null });
      } else {
        results.push({ range: entry.ranges.items[entry.ordinal] });
      }
    }
    return results;
  }

  // ── Internal: CustomXML snapshot storage ────────────────────────────────────

  /** Persist a snapshot as a CustomXML part (the whole RevertSnapshot JSON inside
   *  a single element). Document settings is too small for the full snapshot
   *  (Office BLOCKER-2) so it only ever holds the id. */
  private async storeSnapshot(context: any, snapshot: RevertSnapshot): Promise<void> {
    const xml =
      `<snapshot xmlns="${SNAPSHOT_NAMESPACE}" id="${snapshot.snapshotId}">` +
      `<![CDATA[${JSON.stringify(snapshot)}]]>` +
      `</snapshot>`;
    context.document.customXmlParts.add(xml);
    await context.sync();
  }

  /** Store only the snapshot id in document settings, and persist settings. */
  private setLatestSnapshotId(context: any, snapshotId: string): void {
    const settings = context.document.settings;
    settings.add(SETTINGS_LATEST_SNAPSHOT, snapshotId);
    settings.saveAsync?.(); // best-effort persist (signature varies by host)
  }

  /** Find and parse the CustomXML snapshot for `snapshotId`, or null. */
  private async loadSnapshot(
    context: any,
    snapshotId: string,
  ): Promise<RevertSnapshot | null> {
    const parts = context.document.customXmlParts.getByNamespace(SNAPSHOT_NAMESPACE);
    parts.load('items');
    await context.sync();

    for (const part of parts.items) {
      const xml = part.getXml();
      await context.sync();
      const value = xml.value as string;
      if (!value.includes(`id="${snapshotId}"`)) continue;
      // Extract the CDATA JSON payload.
      const start = value.indexOf('<![CDATA[');
      const end = value.indexOf(']]>');
      if (start < 0 || end < 0) continue;
      const json = value.slice(start + '<![CDATA['.length, end);
      try {
        return JSON.parse(json) as RevertSnapshot;
      } catch {
        return null; // corrupt part — treat as no snapshot
      }
    }
    return null;
  }
}
