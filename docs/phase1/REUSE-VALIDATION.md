# Phase 1 — Reuse validation (measured against the frozen corpus)

Phase 1 answers one question for each reuse candidate from the
[reuse manifest](../REUSE-MANIFEST.md): **does it score against the frozen
corpus?** Clean-room by default — we adopt only what earns it.

Method: the prior converter was run **as-is, in isolation** (loaded directly from
the prior `Mukti-main` sources, no edits) through the same corpus harness used
for the spike. Reproducible by anyone holding the prior ZIPs:

```
node tools/corpus/harness.mjs --dir corpus/visible --converter <adapter-to-prior/bijoy-to-unicode.js>
```

## Result — prior engine (`src/core`, identical across all 5 versions)

| Set | Char accuracy | Word accuracy | NFC fails | Idempotency fails | No-op fails | Fuzz |
|---|---|---|---|---|---|---|
| visible (92) | 96.96% | 96.20% | 5 | 7 | 10 | 233 non-NFC |
| heldout (15) | 99.17% | 96.67% | 0 | 4 | 4 | 233 non-NFC |

**Below the ≥99% gate on word accuracy, and failing idempotency, NFC, and fuzz.**

### Per-category first-pass character accuracy (visible)

| Category | charAcc | Reading |
|---|---|---|
| vowels, consonants, vowel-signs, conjuncts, reordering, digits, words | **1.00** | **map data is correct** |
| sentences | 0.95 | dari + idempotency drag |
| punctuation | 0.83 | **dari (।) broken** → emits literal `|` |
| edge | 0.78 | whitespace munged |
| mixed-script | **0.07** | **URLs/emails mangled** (no protection) |

### What this proves

The failures are **not in the mapping data** — they are in the **processing**:

1. **Idempotency** — `reph-*`, `yaphala-biddya`, `word-desh`, `word-bangladesh`,
   `sentence-long` are all character-correct on the first pass (`ca=1.0`) but fail
   `convert(convert(x))==convert(x)`. The reorder re-fires on its own output —
   the exact bug from the do-not-repeat list.
2. **NFC** — ড়/ঢ়/য় are emitted as the precomposed code points (U+09DC/DD/DF),
   not the NFC-decomposed form; 233/2000 fuzz inputs produce non-NFC output.
3. **Dari** — `।` conversion outputs the literal `|` (the double-escape bug).
4. **Mixed-script** — no URL/email protection, so `https://example.com` becomes
   `যঃঃঢ়ং://বীধসঢ়ষব.পড়স`.
5. **Whitespace** — runs of spaces are collapsed (formatting munging).

## Verdicts (corpus-tested)

| Candidate | Draft verdict | **Phase-1 measured verdict** |
|---|---|---|
| `BIJOY_TO_UNICODE_MAP` (~265 pairs) | ADAPT | **ADAPT — confirmed.** Data is correct (100% first-pass on all core categories). Port to schema-validated JSON; **fix the dari entry**; re-verify the full table in Phase 4. |
| `bijoy-to-unicode.js` pipeline | REWRITE | **REWRITE — confirmed.** Fails the gate on idempotency/NFC/dari/URL/whitespace. |
| `rearrange.js` (reorder) | REWRITE | **REWRITE — confirmed.** Source of the idempotency failures. |
| `normalizer.js` | ADAPT | **REWRITE-lean.** Longest-match compile idea is fine, but no NFC and the dari double-escape make it net-negative; reimplement cleanly. |
| `~138 Bijoy font list` | ADAPT | **ADAPT (not corpus-testable here).** It's a detection-layer asset; validated by the known-font policy at the Office layer (Phase 4), not by this text corpus. Keep the curated names; drop fuzzy MJ-suffix matching. |

## The alternative is already proven

The clean-room Spike B converter (`tools/corpus/reference-converter.mjs`),
authored from cited research rather than the prior code, scores **100% char +
word on both visible and held-out, idempotent, NFC-stable, fuzz-clean**. The
rewrite path is not just necessary — it is demonstrated to work.

**Net Phase 1 outcome:** salvage the mapping **data** (adapt + fix dari + revalidate);
**rewrite all processing** in the TypeScript engine, carrying the design rules
proven in Spike B (idempotency guard, mandatory NFC, URL/email protection,
whitespace preserved).
