/**
 * FROZEN INTERFACE — Mukti Word host adapter (the Office.js seam).
 *
 * PROVISIONAL pending Spikes A (per-run font), C (snapshot revert) and D
 * (encoding seam). These confirm real-Word behaviour the contract assumes; a RED
 * result revises this file before Phase 4 build. This is the ONLY layer allowed
 * to use Office.js. Type-only; no implementation.
 *
 * Behavioural contract (Phase 0 do-not-repeat + Phase 3 review):
 *  - Scope = body + tables (in scope). Headers/footers + footnotes/text boxes/
 *    comments/fields/SmartArt are OUT → reported as `unscanned`, never silently
 *    skipped (M6).
 *  - Font is read per RUN. getTextRanges/split only segment on punctuation/space,
 *    NOT on font boundaries, so a returned range with `fontName: null` (mixed
 *    font) is recursed to finer ranges; anything still null is reported, not
 *    converted (review: Office BLOCKER-1 / Spike A).
 *  - PREVIEW PRODUCES A PLAN; APPLY CONSUMES THAT PLAN (review: arch BLOCKER-1).
 *    Apply re-validates each run's current text against the plan's `before`
 *    before writing, and aborts on mismatch (TOCTOU guard; review: red-team R3).
 *  - Apply snapshots first (changed runs only) into a CustomXML part — NOT
 *    document settings, which is too small (review: Office BLOCKER-2). Revert
 *    re-writes the snapshot. No byte-identical OOXML promise; Ctrl+Z is a
 *    platform fallback only. The snapshot lives in the .docx and travels if the
 *    file is shared — disclosed to the user (review: security F1.3).
 *  - Unknown/Bangla-like-but-unlisted fonts surfaced loudly; never converted (H4).
 *  - Apply writes run-by-run, re-stamping each run's saved formatting (H5/M5).
 */

import type { FontClass } from '../engine/contracts';

// ── Location ───────────────────────────────────────────────────────────────

/** In-scope containers for the MVP. (Headers/footers are reported as unscanned
 *  until Spike A confirms them.) */
export type RegionContainer = 'body' | 'table';

/** A typed, re-findable locator (ranges are not persistable across syncs, so we
 *  re-locate at apply time and re-validate — review: Office MAJOR-3). */
export interface RunLocator {
  readonly container: RegionContainer;
  readonly paragraphIndex: number;
  /** Ordinal of this run's text within the paragraph (disambiguates repeats). */
  readonly ordinal: number;
  /** The exact text expected at this locator, used to detect edits before write. */
  readonly anchorText: string;
}

/** Formatting carried so apply/revert can preserve it (review: arch BLOCKER-2). */
export interface RunFormat {
  readonly fontName: string | null; // null = Word reports no single font (mixed)
  readonly bold?: boolean;
  readonly italic?: boolean;
  readonly size?: number;
  readonly color?: string;
}

/** A homogeneous-font run located during scan. */
export interface RunRef {
  readonly id: string;
  readonly locator: RunLocator;
  readonly text: string;
  readonly format: RunFormat;
}

export type UnscannedKind =
  | 'footnote' | 'endnote' | 'textbox' | 'comment' | 'field' | 'smartart'
  | 'header-footer-pending' | 'mixed-font-unresolved';

export interface UnscannedRegion {
  readonly kind: UnscannedKind;
  readonly count: number;
}

// ── Scan ─────────────────────────────────────────────────────────────────────

export interface ScanReport {
  readonly convertible: ReadonlyArray<RunRef>;   // known Bijoy font
  readonly unsupported: ReadonlyArray<RunRef>;   // Bangla-like, not on the list
  readonly unsupportedFonts: ReadonlyArray<string>;
  readonly unscanned: ReadonlyArray<UnscannedRegion>;
  readonly fontTally: ReadonlyArray<{ fontName: string; class: FontClass; runs: number }>;
}

// ── Plan (preview → apply) ────────────────────────────────────────────────────

/** One planned edit: the engine output for a located run, with its formatting. */
export interface PlannedEdit {
  readonly runId: string;
  readonly locator: RunLocator;
  readonly before: string;     // original text (re-validated at apply time)
  readonly after: string;      // engine output (NFC)
  readonly format: RunFormat;  // re-stamped after the text replace
  readonly changed: boolean;
}

/** The frozen plan: what preview showed is exactly what apply writes. */
export interface ConversionPlan {
  readonly outputFont: string;          // D-0005: Noto Sans Bengali
  readonly dataVersion: string;         // engine mapping-data version (for report)
  readonly edits: ReadonlyArray<PlannedEdit>;
  readonly totalRuns: number;
  readonly changedRuns: number;
}

// ── Apply + Revert ─────────────────────────────────────────────────────────

export interface ApplyResult {
  readonly changedRuns: number;
  readonly snapshotId: string;
  /** Edits skipped because the document changed under us (TOCTOU); reported, not forced. */
  readonly skippedStale: number;
}

/** Stored as a CustomXML part in the .docx (id only kept in settings). */
export interface RevertSnapshot {
  readonly snapshotId: string;
  readonly createdAt: string;
  readonly runs: ReadonlyArray<{ locator: RunLocator; text: string; format: RunFormat }>;
}

// ── The host adapter ─────────────────────────────────────────────────────────

export interface WordHost {
  readonly minRequirementSet: string;       // D-0002 = "1.3"
  isSupported(): boolean;

  scanDocument(): Promise<ScanReport>;
  /** Build the frozen plan (runs the pure engine). No mutation. */
  buildPlan(scan: ScanReport): Promise<ConversionPlan>;
  /** Apply the plan: snapshot changed runs, re-validate each `before`, write run-by-run. */
  applyPlan(plan: ConversionPlan): Promise<ApplyResult>;
  /** Reliable revert: re-write the snapshot. */
  revert(snapshotId: string): Promise<void>;
  /** True if the document still matches the snapshot (warn before reverting — review: a11y R1). */
  documentMatchesSnapshot(snapshotId: string): Promise<boolean>;
  latestSnapshotId(): Promise<string | null>;
}
