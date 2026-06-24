/**
 * Font registry — the known-font classification policy (pure data + logic).
 *
 * Classifies a font NAME only (the host may add byte checks). Membership is
 * EXACT against curated lists after a single normalization (trim + lowercase +
 * collapse internal whitespace). There is deliberately NO fuzzy "MJ-suffix"
 * matching — that produced confirmed false positives (e.g. NikoshMJ, a Unicode
 * font) and silent mangling in the prior code (do-not-repeat H4, finding 02).
 *
 * A name that is NOT on the known lists but LOOKS Bangla-legacy (matches a
 * marker) is classified `unsupported` so the host can warn loudly and leave it
 * untouched. Everything else is `non-bengali` (ignored).
 *
 * Pure TypeScript: ZERO Office.js / DOM. (ARCHITECTURE.md, contracts.ts.)
 */

import type {
  ClassifyFontFn,
  FontClassification,
  FontRegistry,
} from './contracts.js';

/** Trim, lowercase, and collapse internal whitespace to a single space. */
function normalize(name: string): string {
  return name.trim().toLowerCase().replace(/\s+/g, ' ');
}

/**
 * Curated known Bijoy/SutonnyMJ-family font names (normalized).
 * Starter set per finding 02-fonts.md (REUSE verdict ADAPT): the core
 * SutonnyMJ variants plus a representative set of common Ananda Computers
 * Bijoy family names. Deliberately scrubbed of the prior list's miscategorised
 * and over-generic entries (no bare "bangla"/"bijoy", no fuzzy matching).
 * Extend this list as fonts are confirmed; unknown legacy fonts fail loudly.
 */
const KNOWN_BIJOY = [
  'sutonnymj',
  'sutonnymj bold',
  'sutonnymj italic',
  'sutonny mj',
  'sutonnycmj',
  'sutonnyemj',
  'sutonnysushreemj',
  'tonnybanglaj',
  // common Ananda Computers river-named Bijoy fonts
  'gangamj',
  'padmamj',
  'jomunamj',
  'meghnamj',
  'teeshtamj',
  'turagmj',
  'sandipanmj',
  // common newspaper Bijoy fonts
  'jugantormj',
  'samakalmj',
  'jaijaidinmj',
] as const;

/** Names recognised as already-Unicode Bengali (left untouched). */
const KNOWN_UNICODE = [
  'solaimanlipi',
  'noto sans bengali',
  'noto serif bengali',
  'kohinoor bangla',
  'nikosh',
  'nikoshban',
  'kalpurush',
  // SutonnyOMJ is a Unicode-OpenType font despite the MJ-like name (finding 02).
  'sutonnyomj',
] as const;

/**
 * Substrings that flag a name as "Bangla-looking" so an unknown match is
 * surfaced as `unsupported` rather than silently ignored. Lowercase.
 */
const BENGALI_LIKE_MARKERS = ['mj', 'bangla', 'bengali', 'lipi', 'bijoy'] as const;

const BIJOY_SET = new Set<string>(KNOWN_BIJOY);
const UNICODE_SET = new Set<string>(KNOWN_UNICODE);

const UNSUPPORTED_REASON =
  'Looks like a legacy Bangla font but is not on Mukti\'s known list; not converted.';

export const classify: ClassifyFontFn = (fontName: string): FontClassification => {
  const norm = normalize(fontName);

  if (UNICODE_SET.has(norm)) {
    return { fontName, class: 'unicode' };
  }
  if (BIJOY_SET.has(norm)) {
    return { fontName, class: 'bijoy' };
  }
  // Not on either known list: is it Bangla-looking? If so, warn; else ignore.
  const looksBangla = BENGALI_LIKE_MARKERS.some((m) => norm.includes(m));
  if (looksBangla) {
    return { fontName, class: 'unsupported', reason: UNSUPPORTED_REASON };
  }
  return { fontName, class: 'non-bengali' };
};

export const fontRegistry: FontRegistry = {
  classify,
  knownBijoyFonts: KNOWN_BIJOY,
  knownUnicodeFonts: KNOWN_UNICODE,
  bengaliLikeMarkers: BENGALI_LIKE_MARKERS,
};

export default fontRegistry;
