/**
 * Compiles the static Bijoy->Unicode mapping profile.
 *
 * The substitution table lives in `data/bijoy-sutonnymj.json` (schema:
 * `data/schema/mapping-table.schema.json`); reordering stays in code (Spike B).
 * `npm run generate` turns that JSON into `mapping.generated.ts`, which we import
 * here as a plain module — so the engine has NO `node:fs` and bundles cleanly for
 * the Word task pane (browser) while still running under the Node corpus gate.
 * The JSON remains the single schema-validated source of truth. (See
 * ARCHITECTURE.md, D-0009, D-0013.)
 */

import type { MappingEntry } from './contracts.js';
import { profile } from './mapping.generated.js';

export { profile };

/** Turn an entry's code-point array into the source string the engine matches. */
function sourceString(entry: MappingEntry): string {
  return String.fromCodePoint(...entry.source);
}

/**
 * The compiled substitution map: source glyph string -> Unicode target.
 * Built from the data file so the data is the single source of truth.
 */
export const GLYPH_MAP: ReadonlyMap<string, string> = new Map(
  profile.map.map((e) => [sourceString(e), e.target] as const),
);

/** Distinct source-key lengths, longest first, for greedy longest-match. */
export const KEY_LENGTHS: ReadonlyArray<number> = [
  ...new Set([...GLYPH_MAP.keys()].map((k) => k.length)),
].sort((a, b) => b - a);

/**
 * Every single character that can appear in a Bijoy source key. If a run
 * contains none of these it is not Bijoy, so conversion is a guaranteed no-op
 * (the idempotency guard — D-0007).
 */
export const SOURCE_GLYPHS: ReadonlySet<string> = new Set(
  [...GLYPH_MAP.keys()].flatMap((key) => [...key]),
);

export const dataVersion = profile.version;
