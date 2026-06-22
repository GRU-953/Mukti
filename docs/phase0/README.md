# Phase 0 — De-risking

Phase 0 exists to make sure we learn from the past before writing a single line
of production code. It has three jobs:

1. **Forensic root-cause of every prior "Mukti" version → a do-not-repeat list.**
   ✅ **Done.** See [`DO-NOT-REPEAT.md`](DO-NOT-REPEAT.md) and the detailed
   evidence in [`findings/`](findings/).
2. **Build and freeze a gold-standard test corpus.** ⏳ Not started (next).
3. **Spike the three "killer" risks** (per-run font access in Word, Bengali
   cluster reordering correctness, undo fidelity). ⏳ Not started (next).

A *red* spike — one that proves something can't work as assumed — reshapes the
plan before any production code is written.

## What we analysed

You provided five prior versions of "Mukti" as ZIP files:
`Mukti 2.0.0`, `2.5.0`, `2.5.1`, `3.1.0`, and `main`. We extracted and studied
all five. **They are not committed to this repository** (they contain secrets
and proprietary references — see the do-not-repeat list). The analysis below is
the durable record of what we found.

Five independent reviewers each took one area:

| Area | Evidence file |
|---|---|
| Conversion engine (the actual Bijoy→Unicode math) | [`findings/01-engine.md`](findings/01-engine.md) |
| Font detection & the known-font list | [`findings/02-fonts.md`](findings/02-fonts.md) |
| Office.js / Word integration & undo | [`findings/03-officejs.md`](findings/03-officejs.md) |
| Build, manifest, hosting, licensing, **secrets** | [`findings/04-infra.md`](findings/04-infra.md) |
| Tests & the "100% accuracy" claim | [`findings/05-tests.md`](findings/05-tests.md) |

## The one-paragraph summary

All five versions share the **same conversion engine, byte-for-byte** — the
version numbers climbed because of grammar-checking, proofreading, and
multi-app features that are all **out of scope** for our MVP. That shared engine
has real, provable correctness bugs (it is not idempotent, has no Unicode
normalisation, and the Bengali full-stop "dari" conversion is silently broken).
The headline **"100% accuracy across 47 documents"** claim is **not
reproducible and does not measure accuracy** — it only checked that the output
"looked Bengali and didn't crash," against private documents that were never
part of the project. On the safety side there is one **critical** problem (a
real, usable code-signing private key was committed to every version) and a
**licensing** problem (the output font is the proprietary "Kohinoor Bangla").
The good news: **privacy genuinely checks out** (no telemetry, content never
leaves the device), and the core **Bijoy→Unicode character map (~265 pairs) is
worth salvaging** — once independently re-verified.

This is exactly why the plan calls for a clean-room rebuild that *adopts only
what scores* against a frozen corpus, rather than continuing the old codebase.
