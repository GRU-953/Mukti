/**
 * Bengali cluster reordering — the algorithmic passes (Spike B), kept in code
 * rather than data. Bijoy stores some marks in display order; Unicode stores
 * them in logical order, so after the flat glyph substitution we must:
 *   1. collapse an accidental double virama,
 *   2. move reph (র্) to the start of its consonant cluster,
 *   3. move pre-base vowel signs (drawn to the left) after their consonant unit.
 *
 * Pure string logic; no Office.js, no DOM. The regexes are fixed and engine-
 * controlled (no user-supplied patterns), so they are regex-DoS-safe.
 */

const VIRAMA = '্'; // ্
const NUKTA = '়'; // ়

// Bengali consonants block (incl. র). Range U+0995..U+09B9 (ক..হ).
const CONS = 'ক-হ';
// Pre-base vowel signs drawn to the left: i-kar, e-kar, ai-kar.
const PRE_BASE = 'িেৈ'; // ি ে ৈ

// A "consonant unit": a consonant, optional nukta, then any number of
// (virama + consonant) for conjuncts.
const CUNIT = `[${CONS}]${NUKTA}?(?:${VIRAMA}[${CONS}]${NUKTA}?)*`;
const REPH = 'র' + VIRAMA;

const DOUBLE_VIRAMA_RE = new RegExp(VIRAMA + VIRAMA, 'g');
const REPH_RE = new RegExp(`([${CONS}])${REPH}`, 'g');
const PRE_BASE_RE = new RegExp(`([${PRE_BASE}])(${CUNIT})`, 'g');

/** Apply the reordering passes to already-substituted (glyph-mapped) text. */
export function reorder(input: string): string {
  let s = input;
  // 1. collapse accidental double virama (e.g. স্ + ্ব -> স্ব)
  s = s.replace(DOUBLE_VIRAMA_RE, VIRAMA);
  // 2. reph: a consonant immediately followed by র্ -> র্ before that consonant
  s = s.replace(REPH_RE, `${REPH}$1`);
  // 3. pre-base vowels drawn left -> stored after their consonant unit
  s = s.replace(PRE_BASE_RE, '$2$1');
  return s;
}
