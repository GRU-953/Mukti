# Reuse Manifest

> **Status: corpus-tested (Phase 1 done).** The conversion-engine verdicts below
> were validated by running the prior code **as-is, in isolation** against the
> frozen corpus — see [`phase1/REUSE-VALIDATION.md`](phase1/REUSE-VALIDATION.md)
> for the measured numbers. Headline: the prior **map data is correct** (100%
> first-pass on all core categories) but the prior **processing fails the gate**
> (idempotency/NFC/dari/URL/whitespace) → adopt the data, rewrite the pipeline.
> The clean-room Spike B converter already scores 100%, so the rewrite is proven.
> (Office-layer candidates remain to be validated at the Office layer in Phase 4.)
>
> **Provenance correction (Phase 3, D-0013):** "clean-room" means clean-room
> *processing*. The salvaged **map data** is the maintainer's own prior
> **MIT-licensed** Mukti map (© Aninda Sundar Howlader = GRU-953), retained with
> MIT attribution — not third-party. The unlicensed external converter was only a
> disposable cross-check oracle; no code or data from it ships.

**Verdict key:**
- **ADOPT** — bring across with minimal change (still re-verified against corpus).
- **ADAPT** — salvage the data/idea, but clean it up / port to TypeScript / fix bugs.
- **REWRITE** — the shape is a useful blueprint; re-implement from scratch.
- **DISCARD** — out of scope or not worth keeping.

> ⚠️ All five prior versions share the **same conversion engine, byte-for-byte**.
> There is only one engine to judge, not five.

## Conversion engine (`src/core/`)

| Prior artifact | Verdict | Reason / what to salvage |
|---|---|---|
| `mappings.js` → `BIJOY_TO_UNICODE_MAP` (~265 pairs) | **ADAPT** | The single most valuable asset. Port to schema-validated static JSON; fix the escaping bugs; re-verify every pair against an independent oracle. |
| `mappings.js` → pre/post-processing maps | **ADAPT** | Cherry-pick the genuinely Bengali-correctness entries; drop whitespace/formatting-munging rules. |
| `mappings.js` → `UNICODE_TO_BIJOY` | **DISCARD** | Reverse direction is deferred to v2+. |
| `bijoy-to-unicode.js` | **REWRITE** | Pipeline shape (map → rearrange → normalise) is a good blueprint; redo in TS (~50 lines) with NFC + idempotency. |
| `normalizer.js` | **ADAPT** | Longest-first single-pass compile is the right, DoS-safe approach; fix the double-escape bug. |
| `rearrange.js` (cluster reordering) | **REWRITE** | Biggest correctness liability — re-fires on its own output, wrong character classes. This is **Spike #2**. |
| `repair.js` | **DISCARD** | Dead code, never wired into the pipeline. |
| `converter.js` / `multi-converter.js` | **DISCARD** | Shim + dead "multi-encoding" routing (non-Bijoy branches are no-ops). |
| `boishakhi-mappings.js` / `proshika-mappings.js` | **DISCARD** | Empty stubs; the four-family support was illusory. Out of MVP scope anyway. |
| `dictionary.js` | **DISCARD** | Proofreading, network-coupled, unlicensed data, out of scope. |
| `detector.js` + `font-registry.js` → the ~138 Bijoy font names | **ADAPT** | Keep the curated list (scrub miscategorised entries); **discard** the fuzzy MJ-suffix matching and the `|| BIJOY` default. |
| `detector.js` → Unicode allow-list | **ADAPT** | Useful for "already Unicode" detection; make robust to derived variants. |
| Grammar/proofreader (`bengali-grammar-rules.js`, `grammar-checker.js`, `proofreader.js`, `word-analyser.js`, `learning-store.js`) | **DISCARD** | Entirely out of MVP scope. |

## Office.js / host integration (`src/office/`, `src/taskpane/`, `src/commands/`)

| Prior artifact | Verdict | Reason |
|---|---|---|
| `word-processor.js` | **REWRITE** | Per-paragraph font read drops mixed-font text; 3 syncs/paragraph; destructive undo. Useful only as a list of what *not* to do. |
| `excel-processor.js` | **DISCARD (park the pattern)** | Out of scope, but its few-syncs-per-batch model is the perf template for Word. |
| `pptx-processor.js` | **DISCARD** | Out of scope. |
| `taskpane.html` / `taskpane.css` | **ADAPT** | UI scaffold/structure is reusable; rewrite the convert/undo logic and add real preview + bilingual UI. |
| `taskpane.js` (convert/undo logic) | **REWRITE** | No preview; destructive undo. |
| `commands.js` / `commands.html` | **ADAPT plumbing, rewrite body** | Ribbon command wiring is reusable; conversion body is not. |

## Build / infra

| Prior artifact | Verdict | Reason |
|---|---|---|
| `manifest.xml` | **REWRITE** | Word-only, declare WordApi 1.3, custom domain, fresh Id, no localhost. |
| `webpack.config.js`, `jest.config.js`, `.eslintrc.json` | **ADAPT** | Reasonable starting points; retarget to a TypeScript + Node-CI engine with the zero-Office.js-import lint rule. |
| `server/server.js` (dev server) | **ADAPT** | Local dev only; generate certs at runtime, never commit them. |
| `installer/` (Python `install.py` + `__pycache__`) | **DISCARD** | Adds attack surface; sideloading is documented instead. |
| `scripts/` (`curl|bash`, force-push deploy) | **REWRITE** | Insecure (`-k`/`--no-check-certificate`) and self-publishing; replace with CI + documented manual credential setup. |
| `dist-pkg/*.dmg`, `*.zip`, signing keys, certs | **DISCARD / never commit** | Build artifacts and secrets do not belong in git. |

## Tests

| Prior artifact | Verdict | Reason |
|---|---|---|
| Inline `convertBijoyToUnicode("K")→"ক"` equality pairs | **ADOPT (as seeds, re-verify)** | Useful seed cases — but the expected values were authored alongside the map, so re-verify against an independent oracle. |
| `bulk-docx-test.js` | **REWRITE** | Rebuild onto the frozen synthetic corpus with real char/word + formatting diffs and ground truth. |
| Grammar/proofreader test suites | **DISCARD** | Out of scope. |
| Mock-based "mixed-script" integration tests | **DISCARD** | Used mock converters and placeholder characters — proved nothing. |
