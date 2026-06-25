# Gold-Standard Corpus

This is Mukti's frozen, versioned test corpus: the **single source of truth** for
whether a conversion is correct. Everything — the engine, every reuse decision,
the release gate — is measured against it.

## Principles (from the project spec)

1. **No real personal documents.** Every case is synthetic or properly licensed.
   Nothing private ever enters this repository.
2. **Ground truth is independent, not circular.** Expected Unicode is anchored to
   authoritative external references (see `GROUND-TRUTH.md`) and well-known
   correct Bengali spellings — **never** derived from the converter we are
   testing, and **never** from the suspect prior-version map.
3. **Frozen & versioned.** A corpus version is sealed by `MANIFEST.json`
   (SHA-256 of every data file + case counts). Changing a case changes the
   manifest, which is a deliberate, reviewable act.
4. **A held-out slice no engine-builder sees.** `heldout/` is sealed. Agents and
   developers building the converter work **only** against `visible/`. The
   held-out slice is used **only** by the Phase 5 release gate, so the score
   can't be gamed by teaching to the test.

## Layout

```
corpus/
  README.md            ← this file
  GROUND-TRUTH.md      ← how each expected output is justified, with citations
  schema/case.schema.json
  visible/             ← cases the engine may be tested against during development
    *.jsonl
  heldout/             ← SEALED. Do not open while building the converter.
    *.jsonl
  MANIFEST.json        ← freeze record: version, date, sha256 + counts per file
```

## Case format (one JSON object per line, `.jsonl`)

```json
{
  "id": "word-ami-001",
  "category": "words",
  "features": ["vowel-sign", "pre-base-reorder"],
  "source": [65, 118, 119, 103],
  "sourceKeys": "Avwg",
  "expected": "আমি",
  "description": "common word 'ami' (I/me)",
  "ref": "findings/06-mapping-research.md#examples"
}
```

- **`source`** — the exact sequence of **Unicode code points the converter
  receives** when Word hands over legacy text. For SutonnyMJ/Bijoy this is the
  ANSI byte values interpreted as code points. Storing them as an integer array
  (not a literal string) removes *all* file-encoding ambiguity. The harness
  rebuilds the string with `String.fromCodePoint(...source)`.
- **`sourceKeys`** — the human-readable ASCII keystrokes, for review only. Not
  authoritative.
- **`expected`** — the correct Unicode Bengali, **in NFC**. The harness asserts
  `expected === expected.normalize('NFC')` for every case.
- **`features`** — tags so we can score per-feature (e.g. all `reph` cases).
- **`ref`** — where the expected value is justified (a `GROUND-TRUTH.md` anchor
  or `maintainer-verified`).

## Required coverage (per spec)

Bijoy variants · conjuncts · vowel/reph reordering · mixed Bangla/English ·
digits · punctuation (dari ।) · tables · headers/footers · lists · formatted
runs.

> Note: *tables, headers/footers, lists, and formatted runs* are **document
> structure / formatting** concerns that live in the Office layer, not in the
> pure text engine. The text cases here cover the character-level categories;
> the structural categories are exercised by the Office-layer fixtures and the
> formatting-fidelity / preview==commit / revert checks, which are validated on
> real Word during the spikes and Phase 4. This file tracks both so neither is
> forgotten.

## Metrics (computed by `tools/corpus/`)

Pure-text, runnable under Node (no Office.js):

- **Character accuracy** = `1 − CER`, where CER is Levenshtein distance over
  Unicode code points ÷ length of expected (after NFC).
- **Word accuracy** = `1 − WER`, Levenshtein over whitespace-delimited tokens.
- **NFC**: every `expected`, and every converter output, must equal its own NFC.
- **Idempotency**: `convert(convert(source)) === convert(source)`, **and**
  `convert(expected) === expected` (converting already-Unicode text is a no-op).
- **Fuzz**: random/garbage input must never throw and never corrupt non-source
  text (run by the harness in fuzz mode).

Formatting-fidelity, preview==commit, and revert are **Office-layer** gates
(later phases), not part of this pure-text harness.

## Gate

Release requires **≥99%** character *and* word accuracy, **per-font and
overall**, on **both** `visible/` and `heldout/`, with idempotency/fuzz/NFC all
passing. The escape valve (a font shipping below 99%) requires the shortfall to
be documented, runtime-detected, flagged on affected spans, and signed off in
the decision log.
