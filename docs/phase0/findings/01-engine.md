# Mukti Conversion Engine — Forensic Analysis (Engine Core)

Scope: `src/core/` conversion engine only — bijoy-to-unicode.js, mappings.js, rearrange.js,
normalizer.js, converter.js, multi-converter.js, repair.js, boishakhi-mappings.js,
proshika-mappings.js, dictionary.js (role only).

Versions compared: Mukti-2.0.0, 2.5.0, 2.5.1, 3.1.0, main (== 3.1.0). All paths below are
absolute under
`/tmp/claude-0/-home-user-Mukti/01f4b09a-c5f1-5fd5-a600-e11f64dce7a5/scratchpad/prior/`.

## 0. Cross-version note (the engine is FROZEN)

The engine core is **byte-for-byte identical across all five versions** for every in-scope
file except `converter.js`:

| file | 2.0.0 | 2.5.0 | 2.5.1 | 3.1.0 | main |
|---|---|---|---|---|---|
| bijoy-to-unicode.js | 1547B | = | = | = | = |
| mappings.js | 22599B | = | = | = | = |
| rearrange.js | 7518B | = | = | = | = |
| normalizer.js | 1880B | = | = | = | = |
| repair.js | 3539B | = | = | = | = |
| converter.js | 1233B (logic) | 669B (shim) | = | = | = |
| multi-converter.js | absent | 2606B | = | = | = |
| boishakhi/proshika-mappings.js | absent | stub | stub | stub | stub |

So the per-version "improvements" (2.5.x, 3.x) happened in detector/font-registry/
word-analyser/grammar/proofreader — **not** in the conversion math. The actual converter logic
is unchanged since 2.0.0. The "V3 corpus-learned" fixes are baked into `mappings.js` (which is
the same in 2.0.0 too — those comments predate the version split). Judge once; the verdict
applies to all versions.

## 1. Architecture of the conversion pipeline

Entry: `converter.js` → `multi-converter.js` (`convertWithFont`) → `bijoy-to-unicode.js`
(`convertBijoyToUnicode`). The B2U function (main/src/core/bijoy-to-unicode.js:31-50) is a
linear 4-stage pipeline:

```js
text = applyLiteralMap(text, PRE_CONVERSION_MAP);          // Stage 1a: literal split/join
for (const [pattern, replacement] of PRE_CONVERSION_REGEX) // Stage 1b: 3 whitespace regexes
  text = text.replace(pattern, replacement);
text = applyCompiledMap(text, B2U_REGEX, B2U_LOOKUP);      // Stage 2: single-pass big regex
text = rearrangeUnicodeText(text);                         // Stage 3: cluster reorder
text = applyLiteralMap(text, POST_CONVERSION_MAP);         // Stage 4: literal cleanup
```

- Stage 2 regex is pre-compiled once at module load (bijoy-to-unicode.js:24) via
  `compileMapping`, which sorts all keys **longest-first** and alternates them
  (normalizer.js:25-36) so `Av`→আ beats `A`→অ. Good design choice.
- `rearrangeUnicodeText` is a hand-written two-pass index walker over the string
  (rearrange.js:44-185), not regex. Pass 1 = reph + halant/vowel reorder; Pass 2 = pre-kar
  repositioning + composite vowel (ে+া→ো) + nukta.
- **`repair.js` is NOT wired into the pipeline.** `convertBijoyToUnicode` never imports or calls
  `repairBrokenText`. It is dead code with respect to conversion (only referenced elsewhere by
  proofreader UI). So the "double-halant / orphaned-kar repair" never runs during conversion.

## 2. Purity — is the core Node-runnable?

**The seven core conversion files are PURE.** Verified by grep over all `src/core/*.js`: no
`Office`, `Word.`, `window.`, `document.`, `navigator.`, `globalThis`, no DOM. All use CommonJS
`require`. I ran the engine directly under Node with zero shims — it executed fine (see §4).

Exceptions (both **outside** the conversion-core scope):
- `learning-store.js` uses `localStorage` (main/src/core/learning-store.js:27,51,153,163),
  guarded by `typeof localStorage !== "undefined"`. Not imported by the converter.
- `dictionary.js` uses browser `fetch("/data/words.txt")` and `console.log`
  (main/src/core/dictionary.js:23,28,31) — browser-coupled, 454K-word list, lazy-loaded. Used
  only by the proofreader, **not** by conversion.

Conclusion: the conversion engine itself has zero Office.js/DOM coupling. Porting to pure TS is
mechanical (CommonJS→ESM, add types). The *language* is JS, not TS — no types, no schema.

## 3. Cluster reordering — algorithmic, with real correctness gaps

`rearrangeUnicodeText` is **algorithmic** (in-place index walking with `substring` splicing),
not regex. It handles: double-halant collapse (rearrange.js:48); reph `র্` move-before-cluster
(55-93); vowel+halant+consonant swap (96-108); RA+halant+vowel (110-125); pre-kar→post move
with cluster skip (134-166); ে+া→ো / ে+ৗ→ৌ composite (153-158); nukta reposition (169-179).

Correctness gaps:
- **Non-idempotent splicing (the core flaw).** The reph and pre-kar passes mutate `text`
  in-place and adjust `i`, but the move conditions match Unicode output too, so re-running
  re-fires them (proven in §4). The algorithm assumes input is freshly-mapped ASCII-positioned
  Unicode; it corrupts already-correct Unicode.
- **`NUKTA` is mislabeled.** rearrange.js:25 defines `NUKTA = "ঁ"` — that is CHANDRABINDU
  (ঁ), not nukta (nukta is U+09BC). And `BANGLA_CONSONANTS` (line 16) wrongly includes ং ঃ ঁ
  (anusvara/visarga/chandrabindu are not consonants), which skews the reph/cluster walks.
- **Pre-kar pass guards on `!isSpace(next)` only** (rearrange.js:137); a pre-kar immediately
  before a non-consonant non-space (digit, punctuation) still triggers a move with `j=0`,
  producing reordering noise.
- **ো/ৌ composite formation lives in rearrange, not mapping** — fragile; depends on the
  ে-kar and the া/ৗ landing adjacent after consonant-walk, which the buggy consonant set can
  break inside conjuncts.
- Reph "walk back through consonant+halant clusters" (75-82) has no bound on cluster length and
  trusts the consonant set; combined with the chandrabindu-as-consonant bug it can place reph in
  the wrong spot for clusters ending in ং/ঃ.

## 4. Idempotency — BROKEN (proven empirically)

I ran `convertBijoyToUnicode(convertBijoyToUnicode(x))` under Node:

```
"Avwg evsjvq Mvb MvB|" -> "আমি বাংলায় গান গাই|"   twice == once  (OK, but note | not converted)
"ivR©"                 -> "রার্জ"   then "র্রাজ"     NOT IDEMPOTENT (reph re-fires)
"wKQy"                 -> "কিছু"    then "কছিু"      NOT IDEMPOTENT (pre-kar re-fires)
"ÔÕÒÓ"                 -> "‘’“”"    then "্ত্থুচ্চ্"  CATASTROPHIC (Unicode re-consumed as Bijoy)
```

Two independent idempotency failures:
1. **rearrange re-fires** on its own output (reph and pre-kar moves), scrambling clusters.
2. **Mapping keys overlap with Unicode output.** Several `BIJOY_TO_UNICODE_MAP` keys are
   characters the same map *produces*: e.g. U+2018/2019 (`‘ ’`) are output by the smart-quote
   path (mappings.js:111-112 `Ô Õ`→`‘ ’`) yet are also keys mapping to `্তু`/`্থ`
   (mappings.js:157-158); U+201C/201D (`" "`) are output (113-114) but also keys → `চ্`
   (mappings.js:161-162). A second pass eats real Unicode quotes and turns them into conjuncts.

This is a hard blocker for the new "convert twice == once" requirement and must not be carried
over. The fix is a clean separation: detect-source-once, never let output codepoints be input
keys, and make rearrange a pure normalize step (or fold reordering into the mapping so it can't
re-trigger).

## 5. NFC normalization — ABSENT

Grep for `normalize(` / `NFC` / `NFD` across all `src/core/*.js`: **no matches**. The engine
never calls `String.prototype.normalize`. Output NFC-stability is incidental: my test showed
`"আমি বাংলায় গান গাই|"` is **not** NFC-stable (returned false) — the engine can emit
non-canonical sequences (e.g. ো assembled as ে+া rather than the composed form in some paths,
and অা→আ relies on a literal post-fix at mappings.js:290 rather than NFC). The new spec's "NFC
output" requirement is entirely unmet today.

## 6. Regex-DoS risk

- **No user/profile-supplied regex.** All patterns are static literals in `mappings.js` /
  `repair.js`. No `new RegExp` is ever built from user input. (font names go to a lookup table,
  not regex.)
- The big compiled regex (normalizer.js:33) is a pure alternation of escaped literal keys
  (`escapeRegExp`, normalizer.js:54-56) — **no quantifiers, no backreferences, linear**, no
  catastrophic backtracking. Safe.
- `repair.js` patterns are mild (`্্+`, `[‌‍]{2,}`) — linear, safe. (And
  unused, §1.)
- The `PRE_CONVERSION_REGEX` `/ +/g` etc. are linear.
- **Latent bug, not DoS: double-escaping.** `mappings.js` writes three keys pre-escaped as if
  for a regex — `["\\|", "।"]`, `["\\&", ...]`, `["\\^", "্ব"]` (mappings.js:71-73). But
  `compileMapping` runs `escapeRegExp` over them *again*, so the literal lookup key becomes the
  2-char string `\|`, which never matches a bare `|`. Verified: `convert("Mvb|")` → `গান|`
  (the dari `|`→`।` conversion silently does nothing). `^` and `&` also have unescaped duplicate
  entries (mappings.js:241 `^`→ব, plus `Ÿ`/`¡` halant-b), so behavior is partly masked,
  but the dari conversion is genuinely broken.

## 7. Mapping table format

- **JS object/array literals, NOT JSON.** `mappings.js` exports `const BIJOY_TO_UNICODE_MAP = [ ["Av","আ"], ... ]`
  — arrays of `[key, value]` pairs (ordered, ~265 B2U entries + ~245 reverse entries).
  Per-encoding maps (`BOISHAKHI_OVERRIDES`, `PROSHIKA_OVERRIDES`) are also JS arrays.
- **No schema, no validation, no JSON.** Nothing validates key uniqueness, codepoint ranges, or
  output-key disjointness. Comments claim provenance ("Mad-FOX/bijoy2unicode", "bahar/
  BijoyToUnicode") but there is no test fixture proving fidelity.
- **Per-encoding maps are EMPTY STUBS.** `BOISHAKHI_OVERRIDES = []`
  (boishakhi-mappings.js:16-33, all comments) and `PROSHIKA_OVERRIDES = []`
  (proshika-mappings.js:13). And `multi-converter.js` never even applies them: every
  non-Bijoy branch (BOISHAKHI/PROSHIKA/LEKHONI) just calls `convertBijoyToUnicode(text)`
  unchanged (multi-converter.js:40-59) — the "apply overrides then fall through" comments
  describe code that does not exist. So multi-encoding was never functional. (Out of MVP scope
  anyway — confirms nothing salvageable there.)
- Total mapping file: 22.6 KB, 550 lines (both directions).

## 8. REUSE VERDICT per file

| File | Verdict | One-line reason |
|---|---|---|
| `mappings.js` — `BIJOY_TO_UNICODE_MAP` | **ADAPT** | The ~265 Bijoy→Unicode pairs are the genuinely valuable asset; extract to schema-validated static JSON, fix the `\|`/`\&`/`\^` double-escape keys, and de-duplicate output-vs-input codepoints (smart quotes). |
| `mappings.js` — `PRE/POST_CONVERSION_MAP` | **ADAPT** | Useful normalization (অা→আ, double-kar, dari spacing) but entangled with whitespace munging that destroys formatting; cherry-pick the Bengali-correctness entries only. |
| `mappings.js` — `UNICODE_TO_BIJOY_MAP` | **DISCARD** | Reverse direction is out of MVP scope. |
| `bijoy-to-unicode.js` | **REWRITE** | Pipeline shape (pre→map→rearrange→post) is sound and worth keeping as a blueprint, but reimplement in TS with NFC + idempotency guarantees; ~50 lines, cheap to redo. |
| `normalizer.js` (compileMapping/applyCompiledMap) | **ADAPT** | Longest-first single-pass compilation is the right technique and DoS-safe; port to TS, but it currently re-escapes pre-escaped keys — fix that. |
| `rearrange.js` | **REWRITE** | Algorithmic reorderer is the single biggest correctness/idempotency liability (NUKTA mislabel, consonant-set includes ং/ঃ/ঁ, re-fires on own output). Rebuild against a proper Bengali cluster model. |
| `repair.js` | **DISCARD** | Not wired into conversion; a band-aid for breakage a correct engine won't produce. Patterns are trivial to re-derive if ever needed. |
| `converter.js` | **DISCARD** | Thin re-export shim; trivially recreated. |
| `multi-converter.js` | **DISCARD** | Font routing belongs in the new detect-by-font layer; non-Bijoy branches are no-ops and reverse dir is out of scope. |
| `boishakhi-mappings.js` / `proshika-mappings.js` | **DISCARD** | Empty stubs, zero data, out of MVP scope. |
| `dictionary.js` | **DISCARD** (for engine) | Proofreading concern, browser `fetch`-coupled, not conversion; out of scope. |

**Salvage target:** essentially one thing — the `BIJOY_TO_UNICODE_MAP` pair list (and a curated
subset of PRE/POST normalization). Everything else is rewrite or discard.

## 9. Top correctness RISKS — "do-not-repeat" list

1. **Non-idempotent rearrange.** In-place splice passes (reph, pre-kar) re-fire on their own
   output → double-conversion corrupts text. New engine must be provably idempotent.
2. **Output codepoints reused as input keys.** Smart quotes U+2018/2019/201C/201D are both
   produced by and consumed by `BIJOY_TO_UNICODE_MAP` → a second pass mangles real Unicode.
   Input alphabet and output alphabet must be disjoint, or conversion must be gated by source
   detection so it can't re-run on Unicode.
3. **No NFC.** Engine emits non-canonical sequences (ে+া etc., অা patched by literal rule).
   New engine must `normalize("NFC")` as a defined final step.
4. **Wrong character classification in rearrange.** `NUKTA` constant points at চন্দ্রবিন্দু
   (U+0981) not nukta (U+09BC); `BANGLA_CONSONANTS` wrongly includes ং ঃ ঁ. Build the new
   cluster model from a correct, tested Bengali character taxonomy.
5. **Double-escaped mapping keys silently no-op.** `\|`→। , `\&`, `\^` never match because the
   compiler re-escapes them; the dari (`|`→`।`) conversion is broken in production. Keys must be
   raw strings; escaping happens exactly once, at compile time. (Plus: no schema validation, no
   uniqueness/codepoint checks, no fixture proving mapping fidelity — add all three.)
