# Mukti — Migration / Onboarding Pack

**Who this is for.** A future developer — or AI — taking over Mukti. This is the
technical onboarding pack: enough to be productive in an afternoon, and enough to
avoid re-making the mistakes already paid for. Plain language, but technical
detail is welcome here.

For the non-technical, project-level picture (hosting, the bus-factor risk,
credentials, handover), read [`CONTINUITY.md`](CONTINUITY.md) first.

---

## 1. The 60-second mental model

Mukti is a free, MIT-licensed Microsoft Word add-in that converts legacy
**Bijoy / SutonnyMJ** Bangla into proper **Unicode** Bangla — one click, with a
real preview and a reliable revert. Three layers, strict dependency direction:

```
   taskpane UI  (bilingual, accessible)      src/taskpane/ + src/commands/
        │  calls
   host adapter (the ONLY Office.js code)     src/host/
        │  uses
   ENGINE  (pure TypeScript, zero Office.js)  src/engine/
        │  reads
   DATA  (static, schema-validated JSON)      data/
```

- The **engine** is pure TypeScript. It imports **no Office.js**, touches no DOM,
  and runs under plain Node. It is `convert(input: string): string` plus a font
  registry — and that is the part the test set gates directly, with no Word
  present.
- The **host** is the only place allowed to talk to Word (Office.js). It reads
  runs, classifies fonts, builds a plan, applies it, and reverts it.
- The **task pane** is the bilingual (Bangla-default / English) accessible UI.

This shape is the deliberate inverse of the prior versions, where conversion was
tangled into Office.js and could not be tested without Word. Do not undo it. The
purity boundary is **lint-enforced** and will fail the build if you cross it.

The full design is `docs/phase2/ARCHITECTURE.md`. Glossary of every term:
`docs/GLOSSARY.md`.

---

## 2. The frozen contracts — your API to build against

Three artifacts define the fixed shape of the system. Build *behind* them; do not
casually change them (changing them after sign-off is itself a logged decision —
see `docs/phase3/REVIEW.md`).

- **`src/engine/contracts.ts`** — the pure engine + font-registry API. Key
  guarantees baked in:
  - Output is always **NFC**.
  - **Idempotent**: `convert(convert(x)) === convert(x)`, and `convert(x) === x`
    when `x` has no Bijoy source glyphs (D-0007).
  - **Boundary**: the engine *assumes* its input is Bijoy. It cannot tell Bijoy
    bytes from genuine Latin text (Bijoy reuses ASCII slots), so feeding it plain
    English produces garbage *by design*. Deciding what is Bijoy is the **host's**
    font-gating job, never the engine's.
  - Whitespace preserved verbatim; URLs/emails pass through untouched.
- **`src/host/contracts.ts`** — the Word adapter API and its data shapes:
  `scanDocument` → `ScanReport`, `buildPlan` → a frozen `ConversionPlan`,
  `applyPlan` → `ApplyResult`, `revert`. Note the load-bearing rules encoded
  here: **preview produces the plan, apply consumes it** (no re-converting a
  possibly-edited document); apply re-validates each run's `before` text and
  aborts stale edits (TOCTOU guard); the revert snapshot is a **CustomXML part**
  (document settings are too small), changed-runs-only; mixed-font runs that
  cannot be resolved are reported, never converted. This file is **provisional
  pending Spikes A/C/D** (see §6).
- **`data/schema/mapping-table.schema.json`** — the format of the conversion
  table. Data only (code-point arrays + literal replacement strings), no
  user-supplied regular expressions, so it is regex-DoS-safe by construction.

---

## 3. The corpus — spec-by-example and the gate

`corpus/` is the **single source of truth** for whether a conversion is correct.
It is frozen and versioned by `corpus/MANIFEST.json` (a SHA-256 + case count of
every data file). Read `corpus/README.md` and `corpus/GROUND-TRUTH.md`.

It is split deliberately:

- **`corpus/visible/`** — the cases you may build and test against during
  development (consonants, vowel signs, conjuncts, reordering, digits/punctuation,
  mixed script, words, edge cases).
- **`corpus/heldout/`** — **sealed.** It exists only to gate the release, so the
  score cannot be gamed by teaching to the test.

> **Hard rule for engine-builders (human or AI): do NOT read `corpus/heldout/`.**
> Building against it destroys its only purpose. The gate runs it for you.

**Case format** is one JSON object per line (`.jsonl`). `source` is an array of
**Unicode code points the converter receives** (the CP1252 reading of the legacy
font's bytes, stored as integers to remove all file-encoding ambiguity);
`expected` is the correct Unicode, in NFC; `features` tag each case for
per-feature scoring.

**How to run the gate:**

```bash
npm run corpus:gate
```

This builds the engine, runs the harness over **visible and held-out**, and runs
`freeze --check`. It passes only if **character accuracy ≥ 99% AND word accuracy
≥ 99%** (corpus-level CER/WER — Σ edit-distance ÷ Σ length, per-font and overall),
with **zero NFC failures, zero idempotency failures, zero no-op failures**, and
the seeded **fuzz** pass (thousands of random inputs that must never throw and
must always output NFC). `freeze --check` fails if anyone changed corpus data
without a deliberate re-freeze. The same harness lives in `tools/corpus/` and is
wired into CI — green CI means the gate passed.

Note: accuracy is reported as "≥99% on the frozen corpus vN", **never** "99%
accurate" (D-0014). The held-out set is small and same-author, so it is an honest
sanity check, not a statistical guarantee of generalisation.

---

## 4. Extending the conversion map

The conversion table is data, not code. To extend or fix it:

1. **Edit `data/bijoy-sutonnymj.json`** — add/fix entries against
   `data/schema/mapping-table.schema.json`. Each entry is a `source` code-point
   array → a literal `target` string. Reordering (pre-base vowels, reph) is
   **algorithmic** in the engine (`src/engine/reorder.ts`), not in the data — do
   not try to encode reordering as map rows.
2. **Regenerate + gate:**
   ```bash
   npm run generate     # emits src/engine/mapping.generated.ts from the JSON
   npm run corpus:gate  # prove you did not regress
   ```
   `generate` keeps the JSON as the schema-validated source of truth and emits a
   bundle-safe TS module (no `node:fs`), so the same data runs in both the Word
   task pane and the Node gate.
3. If you added genuinely new correct behaviour, you may need to add corpus cases
   and **deliberately re-freeze** (`tools/corpus/freeze.mjs`) — a reviewable act
   that updates `MANIFEST.json`. Never edit corpus data to make a failing engine
   pass.

**The escape valve (R3 / corpus README).** A font that cannot reach 99% after
reasonable effort *may* still ship, but only if all four hold: (1) the shortfall
is documented in `docs/KNOWN-LIMITATIONS.md`, (2) it is detected at runtime, (3)
affected spans are flagged to the user, and (4) it is signed off in
`docs/DECISION-LOG.md`. This keeps Mukti "loud, never silent" even when imperfect.
It is an escape valve, not a lowering of the bar — the default remains ≥99%.

---

## 5. The trail to read first, and the invariants

**Read in this order** before changing anything:

1. `docs/DECISION-LOG.md` — every significant decision and *why*. Start here.
2. `docs/phase3/REVIEW.md` — the adversarial review; what the weak flanks were and
   how they were closed. This is where you learn what almost went wrong.
3. `docs/phase2/ARCHITECTURE.md` and `docs/phase2/BUILD-CI.md` — the layer design
   and the build/CI/security contract.
4. `docs/phase0/DO-NOT-REPEAT.md` and `docs/REUSE-MANIFEST.md` — the forensic
   list of prior mistakes (one shared buggy engine, a committed secret, a
   proprietary font, an unverifiable accuracy claim) and the verdict on every
   salvaged piece.

**Invariants you must not break:**

- **NFC output**, always.
- **Idempotency guard**: never reorder already-Unicode text; no-op on input with
  no Bijoy glyphs (D-0007). This is the exact bug class (`দেশ → দশে` on a second
  pass) that sank the prior engine.
- **Engine purity is lint-enforced.** `src/engine/**` must never import Office.js
  or anything under `src/host`/`src/taskpane`. The rule is in `eslint.config.js`;
  the engine's tsconfig also excludes Office.js types, and the Node-only gate
  would throw if the engine touched `Word`/`Office`. Belt and braces.
- **Loud, never silent.** Unknown/Bangla-looking-but-unlisted fonts are reported
  and left untouched (no fuzzy MJ-suffix matching). Out-of-scope regions are
  reported as `unscanned`, never quietly dropped.
- **Preview = apply.** Apply writes exactly the plan preview produced, and
  re-validates each run before writing.
- **No secrets, ever**, in the repo or the pipeline (D-0004). Dev TLS certs are
  generated at build time into a gitignored path.

---

## 6. Open items a successor inherits

These are known, tracked, and waiting — not surprises.

- **The Word spikes A, C and D must be GREEN before v1.0** (D-0016). They confirm
  real-Word behaviour the host contract *assumes*; until then it is marked
  provisional. Guides in `docs/phase0/spikes/`:
  - **Spike D — the encoding seam (existential, do first).** Does Word's
    `Range.text` actually hand us the CP1252 code points the corpus assumes (e.g.
    the e-kar as `‡` = U+2021), or the raw C1 bytes? Everything rests on this; it
    has never been tested through Word, and the oracle + converter share the
    assumption so their agreement does not prove it. A RED result means
    input-layer rework — the cheapest 15 minutes to spend.
  - **Spike A — per-run font.** Confirm we can read font per *word* inside a
    mixed-font paragraph; unresolved mixed runs are reported, not converted.
  - **Spike C — snapshot revert.** Confirm the snapshot → restore round-trips text
    *and* formatting; Ctrl+Z is an honest platform fallback only.
  - RED on any spike triggers the conservative fallback (selection-only /
    report-don't-convert) and a scope revision.
- **Complete the SutonnyMJ glyph map.** Many conjunct slots beyond the corpus
  subset are still unmapped data work. The loud "unsupported" path + the escape
  valve cover the tail meanwhile.
- **The ZWJ র‍্য gap.** ya-phala after ra needs explicit zero-width-joiner
  handling; currently a known gap.
- **Headers/footers.** Currently reported as `unscanned` (`header-footer-pending`)
  rather than converted; pending the spike result.
- **Pick the custom domain name** before v1.0 (D-0006) — the one outstanding
  maintainer decision.

The rest of the Phase 4 build requirements (per-run char-level font recursion, the
CustomXML snapshot, run-by-run apply preserving format, sync batching with a
measured per-`context.sync()` budget and abortable progress, the accessibility
build, the install guide + manual E2E checklist) are itemised at the end of
`docs/phase3/REVIEW.md`. Treat that list as your backlog.
