/**
 * FROZEN INTERFACE — Mukti conversion engine (pure TypeScript).
 *
 * This file is a type-only contract. It contains NO implementation and MUST NOT
 * import `office-js` or any browser/DOM global — the engine runs under Node and
 * is gated by the corpus harness (see tools/corpus). Phase 4 implements behind
 * these signatures; changing this file after Phase 3 sign-off is a logged
 * decision.
 *
 * Design rules baked into the contract (from Phase 0/1):
 *  - Output is always NFC (Unicode Normalization Form C).
 *  - Idempotent: convert(convert(x)) === convert(x); convert(unicode) === unicode.
 *    Achieved by only transforming runs that contain Bijoy SOURCE glyphs (D-0007).
 *  - Whitespace is preserved verbatim; no formatting/space munging (D-0008).
 *  - URLs / emails inside a run pass through untouched.
 *  - Unknown fonts are NOT the engine's concern to "guess" — the host decides
 *    whether to call the engine, based on the font registry below.
 */

// ──────────────────────────────────────────────────────────────────────────
// Core conversion
// ──────────────────────────────────────────────────────────────────────────

/**
 * Convert a string of Bijoy/SutonnyMJ text to NFC Unicode Bengali.
 * Pure and total: never throws on any input (fuzz-safe); returns NFC.
 * No-op (returns input unchanged, NFC) when the input contains no Bijoy glyphs.
 */
export type ConvertFn = (input: string) => string;

/** True iff `input` contains at least one Bijoy source glyph (i.e. convert() would change it). */
export type IsBijoyTextFn = (input: string) => boolean;

export interface Engine {
  readonly convert: ConvertFn;
  readonly isBijoyText: IsBijoyTextFn;
  /** Mapping-data version the engine was compiled with (for the conversion report). */
  readonly dataVersion: string;
}

// ──────────────────────────────────────────────────────────────────────────
// Font registry — the known-font policy (pure data + logic, no Office.js)
// ──────────────────────────────────────────────────────────────────────────

/** How a font name is classified for conversion routing. */
export type FontClass =
  | 'bijoy'        // a known legacy Bijoy/SutonnyMJ-family font → convert
  | 'unicode'      // already Unicode Bengali (e.g. SolaimanLipi, Noto) → no-op
  | 'unsupported'  // Bangla-looking but NOT on the known list → warn, do NOT touch
  | 'non-bengali'; // ordinary Latin/other font → ignore

export interface FontClassification {
  readonly fontName: string;
  readonly class: FontClass;
  /**
   * For `unsupported`: a human-facing reason ("looks like a legacy Bangla font
   * but is not on Mukti's known list"). The host surfaces this loudly.
   */
  readonly reason?: string;
}

/**
 * Classify a font by name only (no byte heuristics here; the host may add
 * content checks). MUST NOT use fuzzy MJ-suffix matching (do-not-repeat H4) —
 * membership is exact against the curated list, with explicit normalization.
 */
export type ClassifyFontFn = (fontName: string) => FontClassification;

export interface FontRegistry {
  readonly classify: ClassifyFontFn;
  /** The curated, exact known-Bijoy font names (normalized). */
  readonly knownBijoyFonts: ReadonlyArray<string>;
  /** Names recognised as already-Unicode Bengali (left untouched). */
  readonly knownUnicodeFonts: ReadonlyArray<string>;
  /** Substrings/markers that flag a name as "Bangla-looking but unsupported". */
  readonly bengaliLikeMarkers: ReadonlyArray<string>;
}

// ──────────────────────────────────────────────────────────────────────────
// Mapping data (the static JSON the engine compiles; see data/schema)
// ──────────────────────────────────────────────────────────────────────────

/** One substitution row: a Bijoy source glyph sequence → its Unicode component(s). */
export interface MappingEntry {
  /** Bijoy source as an array of code points (editor-safe; matches corpus format). */
  readonly source: ReadonlyArray<number>;
  /** Unicode replacement (authoring form; engine normalizes the final output to NFC). */
  readonly target: string;
  /** Optional note / provenance for the maintainer. */
  readonly note?: string;
}

/** A named, versioned mapping profile (MVP ships exactly one: Bijoy/SutonnyMJ). */
export interface MappingProfile {
  readonly id: string;            // e.g. "bijoy-sutonnymj"
  readonly version: string;       // data version, surfaced in the report
  readonly preMap: ReadonlyArray<MappingEntry>;
  readonly map: ReadonlyArray<MappingEntry>;
  readonly postMap: ReadonlyArray<MappingEntry>;
  /** Reorder is algorithmic (Spike B), not data; this flags which passes apply. */
  readonly reorder: {
    readonly preBaseVowels: boolean;
    readonly reph: boolean;
  };
}
