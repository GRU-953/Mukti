/**
 * FROZEN INTERFACE — Mukti Word host adapter (the Office.js seam).
 *
 * This is the ONLY layer permitted to use Office.js. It turns the document into
 * plain text for the pure engine, and applies/reverts results. Type-only
 * contract; Phase 4 implements behind it. Changing it after Phase 3 sign-off is
 * a logged decision.
 *
 * Behavioural contract (from Phase 0 do-not-repeat + spikes):
 *  - Scope = body + tables (in scope); headers/footers PENDING (Spike A result);
 *    footnotes/text boxes/comments/fields/SmartArt OUT → reported as `unscanned`,
 *    never silently skipped (M6).
 *  - Reads font PER RUN, not per paragraph, so mixed-font paragraphs are not
 *    dropped (H5 / Spike A).
 *  - Preview precedes any mutation (H6).
 *  - Apply snapshots first; Revert restores from the snapshot (H6 / Spike C).
 *    No promise of byte-identical OOXML; Ctrl+Z count is Word-determined.
 *  - Unknown/Bangla-like-but-unlisted fonts surfaced loudly; never converted (H4).
 *  - Performance budgeted per context.sync(); reads/writes are batched (M3).
 */

import type { FontClass } from '../engine/contracts';

// ──────────────────────────────────────────────────────────────────────────
// Where things are in the document
// ──────────────────────────────────────────────────────────────────────────

export type RegionContainer = 'body' | 'table' | 'header' | 'footer';

/** A run of homogeneous-font text that the host located. Ranges are NOT
 *  persisted across syncs — the host re-locates at apply time (feasibility §7). */
export interface RunRef {
  readonly id: string;               // stable within one scan
  readonly container: RegionContainer;
  readonly fontName: string | null;  // null when Word reports no single font
  readonly text: string;
  /** opaque locator the host uses to re-find this run at apply time */
  readonly locator: unknown;
}

/** Region types that are deliberately out of MVP scope and reported, not converted. */
export type UnscannedKind =
  | 'footnote' | 'endnote' | 'textbox' | 'comment' | 'field' | 'smartart'
  | 'header-footer-pending';

export interface UnscannedRegion {
  readonly kind: UnscannedKind;
  readonly count: number;            // how many such regions exist
}

// ──────────────────────────────────────────────────────────────────────────
// Scan
// ──────────────────────────────────────────────────────────────────────────

export interface ScanReport {
  /** Runs whose font is a KNOWN Bijoy font → eligible for conversion. */
  readonly convertible: ReadonlyArray<RunRef>;
  /** Runs in a Bangla-looking but UNSUPPORTED font → warn, do not convert. */
  readonly unsupported: ReadonlyArray<RunRef>;
  /** Distinct unsupported font names, for the loud warning. */
  readonly unsupportedFonts: ReadonlyArray<string>;
  /** Out-of-scope region types present in the document (transparency). */
  readonly unscanned: ReadonlyArray<UnscannedRegion>;
  /** Per-font tally for the conversion report. */
  readonly fontTally: ReadonlyArray<{ fontName: string; class: FontClass; runs: number }>;
  /** context.sync() calls used by the scan (perf budget telemetry, local only). */
  readonly syncCount: number;
}

// ──────────────────────────────────────────────────────────────────────────
// Preview (no mutation)
// ──────────────────────────────────────────────────────────────────────────

export interface PreviewItem {
  readonly runId: string;
  readonly before: string;
  readonly after: string;      // engine output (NFC)
  readonly changed: boolean;
}

export interface Preview {
  readonly items: ReadonlyArray<PreviewItem>;
  readonly totalRuns: number;
  readonly changedRuns: number;
}

// ──────────────────────────────────────────────────────────────────────────
// Apply + Revert
// ──────────────────────────────────────────────────────────────────────────

export interface ApplyResult {
  readonly changedRuns: number;
  /** Output font applied to converted runs (D-0005: Noto Sans Bengali). */
  readonly outputFont: string;
  /** Id of the snapshot stored for "Revert Mukti changes". */
  readonly snapshotId: string;
  readonly syncCount: number;
}

/** Stored (in document settings) so Revert works even after task pane reload. */
export interface RevertSnapshot {
  readonly snapshotId: string;
  readonly createdAt: string;        // ISO date
  readonly runs: ReadonlyArray<{
    readonly locator: unknown;
    readonly text: string;
    readonly fontName: string | null;
    readonly bold?: boolean;
    readonly italic?: boolean;
    readonly size?: number;
    readonly color?: string;
  }>;
}

// ──────────────────────────────────────────────────────────────────────────
// The host adapter
// ──────────────────────────────────────────────────────────────────────────

export interface WordHost {
  /** WordApi requirement set the build targets (D-0002 = "1.3"). */
  readonly minRequirementSet: string;
  /** True iff the running Word supports the required set; UI shows a clear message if not. */
  isSupported(): boolean;

  /** Locate convertible/unsupported runs across body + tables; report unscanned. */
  scanDocument(): Promise<ScanReport>;

  /** Build a before/after preview from a scan, using the pure engine. No mutation. */
  buildPreview(scan: ScanReport): Promise<Preview>;

  /** Apply conversion: snapshot, then replace text + set output font, batched. */
  applyConversion(scan: ScanReport): Promise<ApplyResult>;

  /** Restore the document from the named snapshot (the reliable revert). */
  revert(snapshotId: string): Promise<void>;

  /** The most recent snapshot id, if any (so the UI can offer Revert after reload). */
  latestSnapshotId(): Promise<string | null>;
}
