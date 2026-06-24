// Pure-text accuracy metrics for the Mukti corpus harness.
// Zero Office.js / DOM dependencies — runs under Node and CI.

/** Split a string into an array of Unicode code points (handles astral planes). */
export function codepoints(str) {
  return Array.from(str);
}

/** Levenshtein edit distance over an array of tokens. */
export function levenshtein(a, b) {
  const n = a.length;
  const m = b.length;
  if (n === 0) return m;
  if (m === 0) return n;
  let prev = new Array(m + 1);
  let cur = new Array(m + 1);
  for (let j = 0; j <= m; j++) prev[j] = j;
  for (let i = 1; i <= n; i++) {
    cur[0] = i;
    for (let j = 1; j <= m; j++) {
      const cost = a[i - 1] === b[j - 1] ? 0 : 1;
      cur[j] = Math.min(prev[j] + 1, cur[j - 1] + 1, prev[j - 1] + cost);
    }
    [prev, cur] = [cur, prev];
  }
  return prev[m];
}

const nfc = (s) => s.normalize('NFC');

/**
 * Character accuracy = 1 - CER, where CER = edit distance over code points
 * divided by the length of the expected string (after NFC). Clamped to [0,1].
 */
export function characterAccuracy(expected, actual) {
  const e = codepoints(nfc(expected));
  const a = codepoints(nfc(actual));
  if (e.length === 0) return a.length === 0 ? 1 : 0;
  const dist = levenshtein(e, a);
  return Math.max(0, 1 - dist / e.length);
}

/** Word accuracy = 1 - WER over whitespace-delimited tokens (after NFC). */
export function wordAccuracy(expected, actual) {
  const e = nfc(expected).trim().split(/\s+/).filter(Boolean);
  const a = nfc(actual).trim().split(/\s+/).filter(Boolean);
  if (e.length === 0) return a.length === 0 ? 1 : 0;
  const dist = levenshtein(e, a);
  return Math.max(0, 1 - dist / e.length);
}

/**
 * Raw character stats for CORPUS-LEVEL aggregation: {dist, len} where len is the
 * expected code-point count. Aggregate CER = sum(dist)/sum(len), so each case is
 * weighted by its length (a 48-char sentence counts more than a 1-char case).
 */
export function charStats(expected, actual) {
  const e = codepoints(nfc(expected));
  const a = codepoints(nfc(actual));
  return { dist: levenshtein(e, a), len: e.length };
}

/** Raw word stats for corpus-level aggregation: {dist, len} over tokens. */
export function wordStats(expected, actual) {
  const e = nfc(expected).trim().split(/\s+/).filter(Boolean);
  const a = nfc(actual).trim().split(/\s+/).filter(Boolean);
  return { dist: levenshtein(e, a), len: e.length };
}

/** True iff the string is already in NFC. */
export function isNFC(s) {
  return s === nfc(s);
}

/** Exact match after NFC. */
export function exact(expected, actual) {
  return nfc(expected) === nfc(actual);
}
