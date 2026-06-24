# Phase 3 — Adversarial review (round 1)

Seven independent expert reviewers attacked everything from Phases 0–2. This is
the triage and what changed. Detailed per-lens findings are in
`scratchpad/review/01..07` (not committed — working notes); the durable outcomes
are below and in the commits referenced.

**Reviewers:** Bengali linguistics · Office.js/Word platform · security/privacy/
licensing · architecture/testing · accessibility/UX/i18n · product/process ·
red-team. **Totals:** ~16 blocker-class + ~30 major findings, heavily converging.

## Verdict

The engineering core (pure engine, corpus discipline, privacy intent) held up;
the weak flanks were exactly where independent reviewers converged: **(1) one
untested existential assumption (the encoding seam), (2) the Office.js host
design, (3) distribution to non-technical users, (4) over-claimed accuracy.** All
blockers are now either **fixed** or **converted into a hard gate/requirement**.
No blocker is left open-and-unmanaged → round 1 closes; no round 2 needed.

## Blockers and their resolution

| # | Lens | Blocker | Resolution |
|---|---|---|---|
| R1 | red-team | **Encoding seam untested**: corpus assumes Word's `Range.text` returns CP1252 code points; never verified through Word; oracle+converter share the assumption (circular). | **New Spike D** (encoding kit) + **hard pre-Phase-4 gate** (D-0016). Highest priority. |
| O1 | officejs | Per-run font model wrong: `getTextRanges`/`split` don't split on font boundaries; mixed runs return null. | Host contract: coarse segment → recurse to finer ranges → null-font reported as `mixed-font-unresolved`, never converted. Spike A re-scoped to test intra-word mixing. |
| O2 | officejs | Full revert snapshot can't fit in `document.settings`. | Contract: snapshot = **CustomXML part**, changed-runs-only; settings holds only the id. |
| A1 | arch | **Preview ≠ Apply**: both took only `ScanReport`; apply re-ran convert on a possibly-edited doc. | Contract: **`ConversionPlan`** produced by preview, consumed by apply; apply re-validates each run's `before` and aborts stale edits (TOCTOU guard, also red-team R3). |
| A2 | arch | `RunRef` had no formatting fields → "preserve formatting" unbuildable. | Added `RunFormat` (font/bold/italic/size/color) to `RunRef` and the plan. |
| A3 | arch | "char accuracy" was a per-case mean (1-char case == 48-char sentence). | Harness now reports **corpus-level CER/WER** (Σ dist / Σ len). Verified: identity fn now scores 12%/2.9% (was masked before). |
| L1 | linguistics | Reorder corrupts pre-base-vowel + reph on different consonants. | **Re-tested with real words** (নির্মাণ, বিকর্ষণ, ধর্মে, কার্য): all correct — the agent's failing inputs were synthetic non-words. Added these real cases to the corpus (gap closed). Residual is map *coverage*, a Phase-4 data task, not an algorithm bug. |
| S1 | security | Cross-check oracle has no licence; its map quoted verbatim. | Provenance note + **verbatim code removed**; shipped data derives from the maintainer's own MIT map + specs; oracle is a disposable cross-check (D-0013). |
| P1/P2/P3 | product | No install guide; no real-Word end-to-end acceptance path; sideload-to-end-users under-rated. | Named **release-blocking** deliverables (install guide, manual E2E checklist); distribution path decided (Word-on-web upload + M365 deploy + AppSource endgame); R8 re-rated High (D-0015). |
| U1 | a11y | Revert data-loss trap (no confirm when doc edited after convert). | Contract adds `documentMatchesSnapshot`; UI must confirm before revert. |
| B5 | arch/ling | Idempotency guard treats ASCII as Bijoy → plain English mangled. | Reframed as the **engine's contract boundary**: the engine assumes Bijoy input; detecting Bijoy-vs-Latin is the host's font-gating job. Wording tightened in `engine/contracts.ts`; host-layer negative tests required in Phase 4. |

## What was fixed in this pass (committed)

- **Corpus** expanded 107 → **115** cases (real pre-base+reph words, ন্ত্র
  conjunct, visarga দুঃখ, curly quotes), each triple-checked; re-frozen.
- **Reference converter** gained the missing glyphs; still 100% on visible +
  held-out — now including the hard reorder cases.
- **Harness** switched to corpus-level CER/WER.
- **Frozen contracts** revised: `ConversionPlan`, `RunFormat`, typed `RunLocator`,
  CustomXML snapshot, TOCTOU re-validate, null-font handling; marked provisional
  pending Spikes A/C/D. Engine idempotency boundary clarified; maps optional to
  match the schema. Both type-check clean.
- **Docs**: provenance/licensing note + verbatim-code removed; decision log
  corrected (D-0005 re-filed; D-0006 hard rule; D-0011 CustomXML; new
  D-0012..D-0016); **Spike D** added + A/C/D gate; **SECURITY.md** (CSP,
  self-hosted font, snapshot disclosure, acceptance test); **RUNBOOK.md** started.

## Carried into Phase 4 as requirements (not bugs — build-time work)

1. **Complete the SutonnyMJ glyph map** (the many conjunct slots beyond the
   corpus subset) — mundane data work; loud "unsupported" + escape valve cover
   the tail. Add ZWJ handling for র‍্য (ya-phala after ra) — currently a known gap.
2. **Implement** per-run char-level font recursion; CustomXML snapshot; run-by-run
   apply preserving format; sync batching with a measured per-`context.sync()`
   budget + abortable progress (avoid Word's ~5 s unresponsive kill).
3. **Expand the corpus** with adversarial/real-world cases (ZWNJ/ZWJ, NBSP,
   tracked-changes/fields as *unscanned*, auto-correct artefacts) and **host-layer
   negative tests** (English in a normal font is not sent to the engine).
4. **Accessibility build**: dedicated hidden live-region + focus-order rule; no
   focus on disabled controls; lang-of-parts tagging; min Bangla font size;
   bilingual copy fixes ("SutonnyMJ" label, prefer snapshot-revert over Ctrl+Z,
   don't over-promise "restored", ⌘Z on Mac); replace the placeholder before/after
   sample with a corpus-derived one.
5. **Install guide + manual E2E acceptance checklist + DNS/RUNBOOK** screenshot
   guides for the maintainer.

## Honest limitations recorded (not "fixed")

- **Held-out independence (arch B4):** the held-out set was authored by the same
  process as the visible set, so it measures consistency more than true
  generalisation. Mitigation: it's a sanity check, the gate is phrased "≥99% on
  corpus vN" not "99% accurate" (D-0014), and real-document validation happens via
  the maintainer's E2E checklist. True independence needs a future external
  contributor.
- **Office.js CDN trust:** cannot be SRI-pinned; disclosed.
- The encoding seam (Spike D) is the single assumption the whole project rests
  on; until it is GREEN in real Word, confidence is "high by analogy, not proven".
