/**
 * Public engine API — the pure Bijoy->Unicode core (ZERO Office.js / DOM).
 *
 * This is the module the corpus harness loads (`--converter dist/engine/index.js`)
 * and the host imports. It re-exports `convert` (named + default) so the harness
 * can pick up either, plus the font registry and the engine metadata.
 */

import { convert, isBijoyText, dataVersion } from './convert.js';
import type { Engine } from './contracts.js';

export { convert, isBijoyText, dataVersion };
export { fontRegistry, classify } from './font-registry.js';
export type {
  ConvertFn,
  IsBijoyTextFn,
  Engine,
  FontClass,
  FontClassification,
  ClassifyFontFn,
  FontRegistry,
  MappingEntry,
  MappingProfile,
} from './contracts.js';

/** The assembled engine object (convenience for the host). */
export const engine: Engine = {
  convert,
  isBijoyText,
  dataVersion,
};

export default convert;
