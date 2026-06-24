# 05 — Testing, Corpus & Accuracy Claims (Forensic)

READ-ONLY analysis of all five prior versions under
`/tmp/.../scratchpad/prior/{Mukti-2.0.0, 2.5.0, 2.5.1, 3.1.0, main}`.
Judged against the NEW spec testing gate (frozen gold corpus, held-out slice,
≥99% per-font char/word/formatting accuracy, idempotency/fuzz/preview==commit/revert).

---

## 1. Inventory of test files per version + what they assert

All suites are Jest unit tests on the **pure conversion/grammar string functions**.
`jest.config.js` (all versions) matches **only** `**/test/**/*.test.js`
(`Mukti-main/jest.config.js:3`). Note: `bulk-docx-test.js` does NOT match this glob,
so it is **never run by `npm test`** — it is a standalone script (see §3).

| Version | `*.test.js` files | Actual `test()/it()` count |
|---|---|---|
| 2.0.0 | bugfix, converter | **73** |
| 2.5.0 | + multi-mapping | **103** |
| 2.5.1 | + word-analyser | **146** |
| 3.1.0 | + grammar-checker, grammar-rules, proofreader (+ bulk-docx-test.js non-jest) | **232** |
| main | same as 3.1.0 | **232** |

Counts are `grep -cE "^\s*(test|it)\("` over `*.test.js`. **232 ≠ the claimed "257"** (§3).

What each suite actually asserts (sampled, `Mukti-main`):
- **converter.test.js** (379 ln, 59 tests): single-char/word equality assertions:
  `convertBijoyToUnicode("K")` → `"ক"` (`:36`), kars (`:58`), dari `"\\|"`→`"।"` (`:64`),
  conjuncts ক্ষ/জ্ঞ/ত্র (`:67`), reph (`:250`), 3 roundtrip cases (`:266`), dictionary,
  repair, learning-store, normalizer. Strong **char-level unit coverage of the map tables**.
- **bugfix.test.js** (75 ln, 14 tests): regression for specific compound/reph bugs, e.g.
  `conv("¯Í")`→`স্ত` (`:7`), one reph-reorder check `র্দ` (`:32-36`).
- **multi-mapping.test.js** (222 ln, 30 tests): font-registry classification
  (`getEncodingFamily("SutonnyMJ")`→BIJOY `:16`), "100+ fonts registered" (`:111`),
  7 grammar-word equality pairs (`:205-221`). All MJ fonts share one map (`:172`).
- **word-analyser.test.js** (331 ln, 43 tests): `selectiveConvert` run-segmentation —
  uses **mock converters / placeholder high-byte chars**, not real Bijoy (e.g. `:293`,
  `:314`). Mixed-script "integration" tests pass `(chunk)=>"UNICODE"` mocks (`:319`).
- **grammar-checker / grammar-rules / proofreader** (~675 ln, 86 tests): Bengali
  grammar-rule + proofreader plumbing (export checks, dedup, quality score). **Unrelated
  to conversion accuracy** — they inflate the headline test count.

## 2. Where did test inputs come from? Real personal docs? Binary docx?

- **No `.docx`/`.doc`/`.odt` committed in any version.** Only binaries are build
  artifacts: `*/dist-pkg/Mukti-2.0.0-Windows.zip` (one per version) — not test fixtures.
- **No `fixtures/`, `corpus/`, `gold/`, `sample/`, `test-data/`, or `docs/` directory
  exists in any repo.** All inputs are **inline string literals** in the `.test.js` files.
- **PRIVACY RED FLAG (by claim, not by committed file):** `bulk-docx-test.js` reads
  `path.join(__dirname,"..","..","docs")` (`Mukti-3.1.0/test/bulk-docx-test.js:13`) —
  a directory **one level above the repo root**, absent here. The CHANGELOG calls these
  **"47 real-world documents"** (`Mukti-main/CHANGELOG.md:8`). So the headline metric was
  produced against real (likely personal) documents that live **outside** version control —
  unversioned, unshareable, and unreproducible. No held-out slice; nothing frozen.

## 3. The "47 docs / 20,504 paragraphs / 100%" claim — reproducible? How is "accuracy" computed?

**NOT reproducible.** The harness (`bulk-docx-test.js`) exists but its input `docs/` dir
does not (and is excluded from the repo). `bulk-docx-results.json` is not committed.
It is also **not a Jest test** (wrong filename → never in `npm test`).

"Accuracy" is **NOT char/word accuracy against a gold standard.** It is a heuristic
"looks converted, didn't throw" pass/fail (`Mukti-3.1.0/test/bulk-docx-test.js`):
```
function testConversion(text) {              // :55
  const converted = convert(text, BIJOY_TO_UNICODE);
  const hasBengali = BENGALI_RE.test(converted);            // any U+0980–09FF present?
  const cleaned = converted.replace(LEGIT_PUNCT, "");
  const hasRemnants = REMNANT_RE.test(cleaned);             // leftover high-byte chars?
}
...
if (result.error || (result.hasRemnants && result.hasBengali)) fileFailed++;  // :106
else fileConverted++;
```
A paragraph "passes" if it produced **any** Bengali codepoint and left **no** stray
Latin-1 high bytes — there is **no comparison to expected text**. A garbled but
Bengali-looking output scores as a success. There is **zero ground truth**, so "100%"
measures only "produced Bengali without obvious leftover bytes," not correctness.
The "47/20,504" figures cannot be regenerated. The "257 automated tests" claim
(`CHANGELOG.md:17`) overcounts the real `232` and conflates grammar/proofreader tests
with conversion accuracy.

## 4. Idempotency / fuzz / preview==commit / revert / formatting-fidelity tests?

- **Idempotency:** none. (convert(convert(x))==convert(x) never tested.)
- **Fuzz:** none. The only "Fuzz" hit is `"Fuzzy MJ-suffix matching"` (`multi-mapping.test.js:56`)
  — font-name string matching, not input fuzzing.
- **preview==commit:** none. No Office-document layer is exercised at all.
- **Revert/undo:** none.
- **Formatting fidelity:** none. **Zero tests touch the docx/xlsx/pptx Office layer**
  (no tables, headers, footers, runs, styles). The word "run" in word-analyser tests means
  *text run segment* of a string (`word-analyser.test.js:236,297`), not a Word `w:r`.
- **Roundtrip:** only 3 trivial cases (`converter.test.js:266-282`), char/word level, no docs.

## 5. Frozen / held-out corpus concept? Test-data versioning?

**None.** No frozen corpus, no held-out slice, no manifest/hashing/versioning of test data,
no licensing note. All ground truth is inline literals authored by the same dev who wrote
the mappings (self-referential — a map bug and its test can be wrong together). The only
"corpus" (the 47 docs) is unversioned and outside the repo.

## 6. Coverage gaps vs required categories

| Required category | Status in priors |
|---|---|
| Bijoy variants / multiple MJ fonts | PARTIAL — fonts *classified*, but all share one map; only SutonnyMJ-encoding text actually tested |
| Conjuncts | GOOD (unit) — ক্ষ, জ্ঞ, ত্র, স্ত, গ্রন্থ, প্রোগ্রাম |
| Vowel / reph reordering | THIN — 2-3 reph cases (`bugfix.test.js:32`, `converter.test.js:250`); no systematic reorder matrix |
| Mixed Bangla/English | WEAK — uses **mock converter + placeholder chars**, not real Bijoy (`word-analyser.test.js:310`) |
| Digits | GOOD (unit) — `"123"`→`"১২৩"` |
| Punctuation / dari ।| THIN — single dari case each direction |
| Tables | **MISSING** |
| Headers / footers | **MISSING** |
| Lists | **MISSING** |
| Formatted runs (bold/italic/style preservation) | **MISSING** |
| Per-font measured char/word accuracy | **MISSING** (no metric, no gold) |
| Idempotency / fuzz / preview==commit / revert | **MISSING** |

## 7. REUSE VERDICT (toward a clean-room frozen corpus)

- **ADOPT (as seed string-pairs into a versioned gold file):** the explicit
  `bijoy → unicode` equality assertions in `converter.test.js` (single chars, kars,
  numerals, conjuncts, dari), `bugfix.test.js` compound/reph cases, and the 7 grammar-word
  pairs in `multi-mapping.test.js:205`. These are clean, license-MIT, no PII — good fuzz/
  idempotency anchors. Re-verify each expected value against an independent Unicode source
  (do not trust the original author's expected strings blindly).
- **ADAPT:** font-registry classification tests (`multi-mapping.test.js`) — keep the
  font→family expectations, but add real per-font sample paragraphs.
- **REWRITE:** `bulk-docx-test.js` — concept (corpus over many docs, per-file/overall %)
  is right, but rebuild on a **frozen synthetic .docx corpus with ground-truth pairs** and
  a **real char/word-accuracy + formatting-fidelity diff**, not the "hasBengali && !remnant"
  heuristic. Move it into the Jest run (or a documented `npm run corpus`).
- **DISCARD:** grammar-checker / grammar-rules / proofreader suites for *accuracy-gate*
  purposes (out of scope; keep separately if grammar feature survives). Discard the
  mock-converter mixed-script "integration" tests — they prove segmentation, not conversion.

## 8. Top RISKS for the do-not-repeat list

1. **Unverifiable headline claim.** "100% / 47 docs / 20,504 paras" is not reproducible —
   inputs live outside the repo and aren't committed (`CHANGELOG.md:16`,
   `bulk-docx-test.js:13`). Never assert accuracy without a frozen, in-repo, hashed corpus.
2. **"Accuracy" = "no exception / looks Bengali."** The gate measured presence of Bengali +
   absence of stray bytes, NOT correctness vs ground truth (`bulk-docx-test.js:55-106`).
   Garbage that looks Bengali passes. Future gate MUST diff against expected text.
3. **Real (personal) documents used as the benchmark.** "47 real-world documents"
   (`CHANGELOG.md:8`) — privacy exposure risk and unshareable. Spec demands synthetic/
   licensed only, with a held-out slice. None of that existed.
4. **Inflated/conflated test count.** Claimed 257; real `*.test.js` = 232, of which ~86 are
   grammar/proofreader unrelated to conversion. Headline number oversold rigor.
5. **Zero Office-layer testing.** No tables/headers/footers/lists/formatted-run/
   preview==commit/revert coverage — the exact surfaces that break in production.
6. **No idempotency / fuzz / held-out / versioned corpus** — all four required guardrails absent.
7. **Self-referential ground truth.** Expected strings are inline literals by the map author;
   a wrong mapping and its test can agree. Need independent oracle + held-out data.
