# Phase 0 — The three "killer" spikes

A spike is a small, time-boxed experiment to answer one risky question *before*
building. A **red** spike (the answer is "no / not like that") reshapes the plan.

| # | Risk | Can it be tested in pure code? | Status |
|---|---|---|---|
| **B** | Bengali **cluster reordering** correctness + idempotency | ✅ Yes (Node) | 🟢 **GREEN** — [spike-B-reordering.md](spike-B-reordering.md) |
| **D** | **Encoding seam** — does Word's `Range.text` return the code points our corpus assumes? | ❌ Needs real Word | 🔴 **Highest priority** — [spike-D-encoding.md](spike-D-encoding.md) |
| **A** | Reading the font of each **run** within a paragraph (so mixed-font text isn't dropped) | ❌ Needs real Word | 🟡 Kit ready — [spike-A-per-run-font.md](spike-A-per-run-font.md) |
| **C** | **Undo / revert** fidelity (reliable "Revert Mukti changes") | ❌ Needs real Word | 🟡 Kit ready — [spike-C-undo-revert.md](spike-C-undo-revert.md) |

> **Hard gate (D-0016):** Spikes **A, C and D must be GREEN** (desktop *and*
> Word‑on‑web) before Phase 4 build starts. Spike D is the existential one — it
> checks the assumption every other piece is built on. A RED spike revises scope
> before any production code (conservative fallback: selection‑only /
> report‑don't‑convert).

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
