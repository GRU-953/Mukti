/**
 * The pure Bijoy/SutonnyMJ -> Unicode Bengali conversion engine.
 *
 * Pipeline (faithful port of the proven Spike-B reference converter):
 *   longest-match glyph substitution  ->  reorder (double-virama collapse, reph,
 *   pre-base vowel)  ->  NFC.
 *
 * Guarantees baked into the contract (contracts.ts, D-0007, D-0008):
 *   - Output is always NFC.
 *   - Idempotent: convert(convert(x)) === convert(x); and convert(x) === x for
 *     any x with no Bijoy source glyphs (already-Unicode is a no-op).
 *   - Whitespace preserved verbatim (no tidying).
 *   - URLs / emails pass through untouched.
 *   - Pure and total: never throws on any input (fuzz-safe). ZERO Office.js/DOM.
 */

import type { ConvertFn, IsBijoyTextFn } from './contracts.js';
import { GLYPH_MAP, KEY_LENGTHS, SOURCE_GLYPHS, dataVersion } from './mapping-data.js';
import { reorder } from './reorder.js';

export { dataVersion };

// URL / email runs are passed through untouched (their ASCII must not be
// remapped through the Bijoy table). Fixed, engine-controlled pattern.
const PROTECT =
  /([A-Za-z][A-Za-z0-9+.-]*:\/\/[^\s]+|www\.[^\s]+|[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,})/g;

/** Greedy longest-match substitution of Bijoy source glyphs to Unicode. */
function mapGlyphs(text: string): string {
  let out = '';
  let i = 0;
  while (i < text.length) {
    let matched = false;
    for (const len of KEY_LENGTHS) {
      const slice = text.substr(i, len);
      const target = GLYPH_MAP.get(slice);
      if (target !== undefined) {
        out += target;
        i += len;
        matched = true;
        break;
      }
    }
    if (!matched) {
      out += text[i];
      i += 1;
    }
  }
  return out;
}

/** True iff `input` contains at least one Bijoy source glyph. */
export const isBijoyText: IsBijoyTextFn = (input: string): boolean => {
  for (const ch of input) {
    if (SOURCE_GLYPHS.has(ch)) return true;
  }
  return false;
};

/** Convert a Bijoy/SutonnyMJ string to NFC Unicode Bengali. */
export const convert: ConvertFn = (input: string): string => {
  if (!input) return input;
  // Idempotency / no-op guard: nothing to convert without Bijoy glyphs (D-0007).
  if (!isBijoyText(input)) return input.normalize('NFC');

  // Protect URLs/emails: split keeps the captured runs at odd indices.
  const parts = input.split(PROTECT);
  let out = '';
  for (let k = 0; k < parts.length; k++) {
    const seg = parts[k] ?? '';
    if (k % 2 === 1) {
      out += seg; // protected run — verbatim
    } else {
      out += reorder(mapGlyphs(seg));
    }
  }
  return out.normalize('NFC');
};

export default convert;
