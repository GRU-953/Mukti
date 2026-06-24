# Phase 0 — The three "killer" spikes

A spike is a small, time-boxed experiment to answer one risky question *before*
building. A **red** spike (the answer is "no / not like that") reshapes the plan.

| # | Risk | Can it be tested in pure code? | Status |
|---|---|---|---|
| **B** | Bengali **cluster reordering** correctness + idempotency | ✅ Yes (Node) | 🟢 **GREEN** — [spike-B-reordering.md](spike-B-reordering.md) |
| **D** | **Encoding seam** — does Word's `Range.text` return the code points our corpus assumes? | ❌ Needs real Word | 🟡 **High confidence** (research) — kit: [spike-D-encoding.md](spike-D-encoding.md); evidence: [spike-D-evidence.md](spike-D-evidence.md) |
| **A** | Reading the font of each **run** within a paragraph (so mixed-font text isn't dropped) | ❌ Needs real Word | 🟡 Kit + evidence — [spike-A-per-run-font.md](spike-A-per-run-font.md), [spike-AC-evidence.md](spike-AC-evidence.md) |
| **C** | **Undo / revert** fidelity (reliable "Revert Mukti changes") | ❌ Needs real Word | 🟡 Kit + evidence — [spike-C-undo-revert.md](spike-C-undo-revert.md), [spike-AC-evidence.md](spike-AC-evidence.md) |

> **Gate (D-0016):** Spikes **A, C and D must be GREEN** (desktop *and*
> Word‑on‑web) before the v1.0 **release**. Per the maintainer's choice the build
> proceeded in parallel. Authoritative research now rates all three **high
> confidence** (see the evidence docs); the in‑Word kits are the final
> confirmation. A RED result triggers the conservative fallback (selection‑only /
> report‑don't‑convert).
>
> **Correction from the A/C evidence:** `Word.Document.customXmlParts` / `.settings`
> (where the revert snapshot lives) are **WordApi 1.4**, not 1.3. To stay at the
> declared 1.3 the host uses the **Common API** `Office.context.document` surface
> for snapshot storage (or the manifest is raised to 1.4). See D‑0017.

## Why A and C are "kits" not results

Spikes A and C exercise live Microsoft Word behaviour (the Office.js host), which
cannot be run in this build environment. Each has a ready-to-run **Script Lab**
snippet and a step-by-step, non-technical guide, designed to **self-verify** (the
snippet feature-detects the APIs and prints a clear GREEN/RED line) so the result
doesn't depend on anyone's memory of the API.

Both are also **partially de-risked already** by the Phase 0 forensic evidence:
- A: the prior versions provably read `paragraph.font.name` in production across
  every release, so reading a font from a homogeneous range works. The open
  question the kit answers is the finer **per-run** granularity.
- C: the prior versions provably landed `insertText` edits on Word's native undo
  stack. The open question the kit answers is whether a **snapshot→restore**
  revert round-trips formatting faithfully.

So the residual risk is low; the kits convert it to certainty with one ~15-minute
session in Word. See each kit for how to run it and what to send back.
