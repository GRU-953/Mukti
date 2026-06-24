/**
 * Loads and compiles the static Bijoy->Unicode mapping profile.
 *
 * The substitution table lives in `data/bijoy-sutonnymj.json` (schema:
 * `data/schema/mapping-table.schema.json`); reordering stays in code (Spike B).
 * The data is read once at module load via the filesystem so the engine carries
 * no bundler/JSON-import assumptions and stays a pure Node leaf module — no
 * Office.js, no DOM. (See ARCHITECTURE.md, D-0009, D-0013.)
 */

import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { dirname, join } from 'node:path';
import type { MappingEntry, MappingProfile } from './contracts.js';

/** Resolve `data/bijoy-sutonnymj.json` relative to this module, in src or dist. */
function locateProfile(): string {
  const here = dirname(fileURLToPath(import.meta.url));
  // From `src/engine/` -> `../../data`; from `dist/engine/` -> `../../data`.
  return join(here, '..', '..', 'data', 'bijoy-sutonnymj.json');
}

function loadProfile(): MappingProfile {
  const raw = readFileSync(locateProfile(), 'utf8');
  return JSON.parse(raw) as MappingProfile;
}

export const profile: MappingProfile = loadProfile();

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
