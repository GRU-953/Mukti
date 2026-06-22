# Mukti Forensic Analysis — Area 02: Font Detection & Known-Font List

Scope: `src/core/detector.js`, `src/core/font-registry.js`, `src/core/multi-converter.js`, and how these wire into the Word/Excel/PPT processors. Evidence cited against the `Mukti-main` tree (identical font code in 2.5.0/2.5.1/3.1.0/main) with evolution notes from 2.0.0.

---

## 0. Version evolution at a glance

| Version | Detection mechanism | Font list |
|---|---|---|
| **2.0.0** | Inline `BIJOY_FONT_NAMES` Set (exact-match, ~40 names) in `detector.js`; STRICT font gate: `isBijoyFont(fontName) && !isAlreadyUnicode(text)` (`Mukti-2.0.0/src/office/word-processor.js:82`). No fuzzy match, no registry. | ~40 hardcoded names, single flat Set. SutonnyOMJ classified as **Bijoy** here (it's in the 2.0.0 Bijoy set, `detector.js:123`). |
| **2.5.0** | Introduces `font-registry.js` with 4 encoding families + **fuzzy MJ-suffix matching**. `detector.isBijoyFont` now delegates to `isBengaliAnsiFont`. SutonnyOMJ **moved to Unicode list** + added to a fuzzy exclusion list. | 138-entry registry Map. |
| **2.5.1** | Identical font-registry/detector to 2.5.0 (byte-for-byte). CHANGELOG claims "word-by-word detection", "138+ fonts", "fuzzy MJ-suffix matching", "Automatic SutonnyOMJ Unicode classification" — all already present in 2.5.0 code. | Same 138 entries. |
| **3.1.0 / main** | Identical font-registry.js and detector.js to 2.5.0/2.5.1. Word processor adds **full-paragraph conversion when font is confirmed Bijoy** (`word-processor.js:92`) and word-by-word `selectiveConvert` for mixed fonts. | Same 138 entries. |

`font-registry.js` is **byte-identical** across 2.5.0, 2.5.1, and main (diff confirms zero changes). The "138+ fonts" / "fuzzy MJ matching" / "SutonnyOMJ" features advertised under 2.5.1 in the CHANGELOG actually all landed in 2.5.0.

---

## 1. How is the source font detected? (font-name property, byte heuristic, or both?)

**Both — combined as an AND gate, evaluated per paragraph (not per run/per document).**

In `Mukti-main/src/office/word-processor.js:56-105` `convertParagraph()`:
1. Loads the Office font-name property: `paragraph.font.name` (`word-processor.js:64`).
2. **Font-name gate** (the authoritative source decision): `if (isB2U && !isBijoyFont(fontName)) { result.skipped++; return; }` (`word-processor.js:70`).
3. **Byte heuristic pre-check**: `if (isB2U && !hasConvertibleContent(text))` — `hasConvertibleContent` returns true only if the text contains a Bijoy high-byte char (`word-analyser.js:204-207`, via `isBijoyHighByte`, `detector.js:33-42`).

So the **font name decides whether to process the paragraph at all**; the byte heuristic only confirms there is actually convertible content. There is **no per-run iteration** — the code reads a single `paragraph.font.name`. For a paragraph with mixed fonts, Office returns `null` for `.font.name`, which fails `isBijoyFont(null)` → the whole paragraph is silently skipped (`result.skipped++`). This is a real gap vs. the spec's "detect source by FONT NAME per run."

Two-layer detector primitives in `detector.js`:
- `detectScript()` (`detector.js:55-76`) — byte-based script classifier (Bengali Unicode vs Bijoy high-byte vs Latin). **Not used in the conversion gate**; only a utility.
- `isBijoyFont()` → `isBengaliAnsiFont()` → `getEncodingFamily()` (`font-registry.js:126`) — the font-name classifier that actually gates conversion.

Note: at the **token level inside `selectiveConvert` the font is ignored entirely** — `tokenize`/`selectiveConvert` (`word-analyser.js:70-198`) classify tokens purely by byte content (`hasBijoyContent`) and convert any token with a high-byte char. Font only filters at the paragraph boundary. See Risk #2.

---

## 2. The "fuzzy MJ-suffix matching" — what it is and its false-positive risk

`getEncodingFamily()` in `font-registry.js:126-159`:
1. Direct registry lookup of the normalized (trim+lowercase) name (`:131`).
2. Strip a trailing weight/style word (`regular|bold|italic|...`) and retry the lookup (`:137-143`).
3. Exclusion list `["sutonnyomj"]` → return null (`:147-150`).
4. **Fuzzy fallback**: `if (/(?:mj|cmj|emj|pmj|nmj|tmj)$/i.test(stripped)) return EncodingFamily.BIJOY;` (`:154-156`).

So **any font name ending in the letters "mj"** (after stripping a recognized weight word) is classified as Bijoy, even if it is not in the registry.

**Confirmed false positives** (executed against main's registry):
```
"MyFontMJ"          -> bijoy   (not a real Bijoy font)
"RandomMj"          -> bijoy
"NikoshMJ"          -> bijoy   (Nikosh is a UNICODE font; an "MJ" variant name would mangle)
"SomeUnicodeFontMJ" -> bijoy
"XYZ EMJ"           -> bijoy
"XSutonnyOMJ"       -> bijoy   (exclusion only catches exact "sutonnyomj")
```
The pattern matches the substring "mj" at end-of-string with no word boundary, so it also matches names like `...omj`, `...emj` that are NOT Bijoy. **Risk: a Unicode (or other-encoding) Bengali font whose name happens to end in "mj" is treated as Bijoy and its already-correct text gets byte-remapped → mojibake.** This directly violates the spec requirement that unsupported fonts "FAIL LOUDLY, never silently mangle."

---

## 3. The hardcoded known-font list

Yes — `FONT_FAMILY_REGISTRY` Map in `font-registry.js:22-120`. **Exactly 138 entries** (verified by loading the module): `bijoy: 114, boishakhi: 5, proshika: 14, lekhoni: 5`.

- **Bijoy family** (`BIJOY_FONTS`, `font-registry.js:26-84`, 114 names): core SutonnyMJ variants (`sutonnymj`, `sutonny`, `sutonnymj bold`, `sutonnymjbold`, `sutonnycmj`, `sutonnyemj`, `sutonnysushreemj`, `sutonnysushreeomj`, `tonnybanglaj`); ~50 river-named Ananda Computers fonts (`gangamj`, `padmamj`, `jomunamj`, `teeshtamj`, `turagmj`…); decorative MJ fonts; newspaper fonts (`jugantormj`, `samakalmj`, `jaijaidinmj`); CMJ variants; and loose names like `boishakhi`, `bangla`, `bijoy`, `tonni`, `tutul`, `shapla`, `somewherein`, `bornosoft`.
- **Boishakhi** (`:87-90`, 5): `shorif boishakhi`, `boishakhimj`, `boishakhi regular/bold/italic`.
- **Proshika** (`:93-100`, 14): `adarshalipinormal`(+weights), `adarshalipexp`, `lipi karabib/dalia/palash`, `sulekha`, `proshikashabda`…
- **Lekhoni** (`:103-106`, 5): `lekhoni`(+weights), `prothom alo`, `prothomalo`.

Separately, **Unicode** allow-list `UNICODE_BENGALI_FONT_NAMES` (`detector.js:119-131`, 20 entries): `solaimanlipi`, `kohinoor bangla`(+weights), `kalpurush`, `nikosh`, `nikoshban`, `noto sans/serif bengali`, `vrinda`, `aparajita`, `mukti narrow`, `akaash`, and notably `sutonnyomj`(+bold/italic).

**Data-integrity bug:** `BIJOY_FONTS` includes `"boishakhi"` (`:77`) while `BOISHAKHI_FONTS` includes `"boishakhimj"`, `"shorif boishakhi"` etc. — the plain `boishakhi` name is mapped to the **BIJOY** family, so a true Boishakhi-encoded document tagged plainly "Boishakhi" would be routed through the Bijoy mapping table. (Largely moot today — see Risk #4: all families fall through to the Bijoy pipeline anyway.)

---

## 4. What happens on an UNKNOWN font — loud failure or silent passthrough?

**Mostly silent skip; in one path, silent mangling. Never a loud, surfaced failure for full-document conversion.**

- **Full-document / table / header-footer path** (`word-processor.js:70`, `:257`, `:280-291`): an unknown font fails `isBijoyFont` and the paragraph is silently dropped via `result.skipped++` (or, in headers/footers, an empty `catch {}` at `:280`/`:289`). The result object only has a generic `skipped` counter; it never distinguishes "skipped because unknown font" from "skipped because empty/already-unicode." The UI panel tries to show `result.fontGateSkipped` (`taskpane.js:276-282`) but **the processors never set `fontGateSkipped`** — only `skipped` — so this warning is **dead code** for the conversion result; the user sees no per-font reason. (A separate *preview-scan* path in taskpane.js does count `fontGateSkipped` at `:702/:744/:777/:838`, but that is a different scan, not the conversion result.)

- **Selection path** does surface a message (the closest thing to "loud"):
  `return { converted: false, error: "Selected text is not in a Bijoy/SutonnyMJ font (detected: " + (fontName || "unknown") + ")." };` (`word-processor.js:202-203`). Clear, but only for explicit selection conversion.

- **Silent-mangle path (the dangerous one):** once the paragraph passes the font gate, `convertWithFont` (`multi-converter.js:29-70`) computes
  `const encoding = fontName ? getEncodingFamily(fontName) || EncodingFamily.BIJOY : EncodingFamily.BIJOY;` (`:32-34`).
  The `|| EncodingFamily.BIJOY` **defaults any unresolved/unknown font to the Bijoy mapping**. Combined with the fuzzy-MJ false positives (§2), a non-Bijoy font that slips through the gate is byte-remapped through the Bijoy table with **no warning** — silent corruption. There is **no `throw`/error for an unknown-but-passed font**; the only `throw` in this file is for an unknown conversion *direction* (`:66`).

**Verdict against spec:** FAILS the "unsupported fonts must FAIL LOUDLY, never silently mangle" requirement. Unknown fonts are silently skipped (whole-doc) or silently coerced to Bijoy (post-gate / fuzzy false-positive).

---

## 5. Classification of "SutonnyOMJ" as already-Unicode — how robust?

Two redundant mechanisms, only moderately robust:
1. `UNICODE_BENGALI_FONT_NAMES` includes `"sutonnyomj"`, `"sutonnyomj bold"`, `"sutonnyomj italic"` (`detector.js:129-130`) → `isUnicodeBengaliFont("SutonnyOMJ") === true`.
2. `getEncodingFamily` exclusion list `UNICODE_EXCLUSIONS = ["sutonnyomj"]` (`font-registry.js:147-150`) → returns null, so it is NOT treated as ANSI/Bijoy.

**Robustness holes (verified):**
- Works for exact `sutonnyomj` and for variants whose suffix is in the strip list (`SutonnyOMJ Bold/Italic/Light/Heavy/Black/Semibold` all → null, because they strip to `sutonnyomj` then hit the exclusion). Good.
- **Breaks for unrecognized suffixes**: `"SutonnyOMJ Condensed"` strips nothing (condensed not in the regex), so the exclusion isn't hit — it survives only by luck because `"sutonnyomj condensed"` doesn't end in "mj" → null. Fragile coincidence, not intent.
- **Breaks for prefixed/derived names**: `"XSutonnyOMJ"` → **bijoy** (exclusion is an exact-string `includes`, the fuzzy `…mj$` then fires). Any vendor-renamed OMJ variant ending in "omj"/"emj" that isn't the literal `sutonnyomj` is misclassified as Bijoy and would be mangled.
- The exclusion is a **single hardcoded special-case**, not a principled rule. It also presumes SutonnyOMJ is Unicode-OpenType; that classification is asserted in comments (`detector.js:129`, `font-registry.js:145-146`) with no test fixture validating real SutonnyOMJ bytes.

---

## 6. REUSE VERDICT (ADOPT / ADAPT / REWRITE / DISCARD)

| Component | Verdict | Rationale |
|---|---|---|
| **138-entry Bijoy font name list** (`BIJOY_FONTS`) | **ADAPT** | The curated names (esp. SutonnyMJ/CMJ/EMJ variants + Ananda river fonts) are real domain knowledge worth salvaging. But scrub it: remove non-Bijoy entries miscategorized into Bijoy (`boishakhi`, and overly generic `bangla`/`bijoy`), and make it a versioned, test-backed allow-list. MVP only needs the SutonnyMJ/Bijoy subset. |
| **Exact-match `Set`/`Map` lookup** (normalize → lookup) | **ADOPT** | Trim+lowercase normalization + Map lookup is the right primitive and matches the spec's "DEFINED known-font list." |
| **Fuzzy MJ-suffix matching** (`/(?:mj\|cmj\|...)$/`) | **DISCARD** | Direct violation of "unsupported fonts must fail loudly." It guesses, produces confirmed false positives (NikoshMJ, anything ending "mj"), and is the mechanism that turns unknown fonts into silent Bijoy mangling. Replace with explicit allow-list; unknown = loud fail. |
| **`getEncodingFamily` `\|\| BIJOY` default in multi-converter** (`multi-converter.js:33`) | **DISCARD / REWRITE** | The silent fallback-to-Bijoy is the core silent-mangle hazard. For MVP must become: unknown font → no conversion + reported reason. Also: all four families currently route to `convertBijoyToUnicode` anyway (`:43-59`), so multi-encoding is illusory — for a Bijoy-only MVP, collapse to a single Bijoy converter and DISCARD the family-routing scaffolding. |
| **SutonnyOMJ exclusion** (single hardcoded string) | **ADAPT** | Keep the *intent* (don't convert SutonnyOMJ) but implement as a first-class Unicode allow-list entry validated by a byte/fixture test, not a fuzzy-list patch. |
| **`detectScript` / byte heuristics** (`detector.js`) | **ADOPT (as confirmation only)** | Useful as a secondary "does this text actually contain Bijoy bytes" guard (`hasConvertibleContent`), but must never be the primary source decision — keep font-name as authoritative per spec. |
| **Paragraph-level font gate** (single `.font.name`) | **REWRITE** | Must move to **per-run** detection. Reading one `paragraph.font.name` mishandles mixed-font paragraphs (Office returns `null` → whole paragraph skipped) and contradicts the spec's per-run requirement. |

---

## 7. Top RISKS for the do-not-repeat list

1. **Fuzzy `…mj$` false positives → silent mangling.** Any Bengali (or arbitrary) font ending in "mj" is force-classified Bijoy (`font-registry.js:154`); verified for `NikoshMJ`, `MyFontMJ`, `XSutonnyOMJ`, `SomeUnicodeFontMJ`. A Unicode font caught this way gets byte-remapped to garbage. **Hard fail of the "never silently mangle" rule.**

2. **`getEncodingFamily(font) || BIJOY` default** (`multi-converter.js:33`) coerces every unresolved font to Bijoy after the gate — no warning, no error. Combined with #1, unknown fonts are corrupted rather than reported.

3. **Unknown fonts are silently skipped, with no surfaced reason.** Full-doc path only does `result.skipped++` (`word-processor.js:70`); the `fontGateSkipped` UI counter is **never populated by the conversion processors** (dead code at `taskpane.js:276`). The user cannot tell "skipped: unsupported font X" from "skipped: empty." Spec requires a clear unsupported-font message.

4. **Out-of-scope regions silently ignored, not reported as "not scanned."** `processWordDocument` walks only `body.paragraphs` + `body.tables` + headers/footers (`word-processor.js:138/163/164`). **No handling and no "not scanned" notice** for footnotes, endnotes, text boxes, comments, fields, or SmartArt — they are silently absent. Spec explicitly requires these be reported as "not scanned."

5. **Mixed-font paragraphs dropped.** Detection reads a single `paragraph.font.name`; for multi-font paragraphs Office returns `null`, failing `isBijoyFont` and skipping the entire paragraph. Token-level `selectiveConvert` then ignores font entirely and converts by bytes — an inconsistent two-headed model (font-gated at paragraph, byte-gated at token).

6. **Family mislabeling / illusory multi-encoding.** Plain `"boishakhi"` is mapped to the BIJOY family (`font-registry.js:77`), and all four families fall through to the same `convertBijoyToUnicode` (`multi-converter.js:43-59`) — so "multi-encoding support" is cosmetic. Don't ship encoding families that all alias to Bijoy; for MVP scope to Bijoy/SutonnyMJ explicitly (matches spec) and defer the others honestly.

7. **SutonnyOMJ special-case is brittle.** A lone hardcoded `["sutonnyomj"]` exclusion (`font-registry.js:147`) guards a real correctness case but breaks on any unrecognized weight/prefix variant (`XSutonnyOMJ` → bijoy). No fixture test validates the underlying "OMJ is Unicode" assumption.
