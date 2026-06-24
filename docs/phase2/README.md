# Phase 2 — Design & risk register

The blueprint Mukti is built against. **No production code yet** — this phase
fixes the architecture, the frozen interface contracts, and the risks, so that
the Phase 3 adversarial review has something concrete to attack and Phase 4 has
fixed seams to build behind.

## Contents

| Document | What it fixes |
|---|---|
| [ARCHITECTURE.md](ARCHITECTURE.md) | The layered design (pure engine ↔ Office host ↔ UI), module boundaries, data flow for one conversion, and where the frozen interfaces live. |
| [`src/engine/contracts.ts`](../../src/engine/contracts.ts) | **Frozen interface** — pure engine + font-registry + mapping-data types (zero Office.js). |
| [`src/host/contracts.ts`](../../src/host/contracts.ts) | **Frozen interface** — the Word host adapter (scan/preview/apply/revert) and its data shapes. |
| [`data/schema/mapping-table.schema.json`](../../data/schema/mapping-table.schema.json) | The static, schema-validated, regex-DoS-safe mapping-data format. |
| [MANIFEST-DESIGN.md](MANIFEST-DESIGN.md) | Word-only manifest, WordApi 1.3 declared, custom-domain strategy, fresh GUID, re-sideload rule. |
| [UI-UX.md](UI-UX.md) | Bilingual (Bangla-default) accessible taskpane: states, wireframes, full string table, keyboard map, WCAG 2.1 AA. |
| [RISK-REGISTER.md](RISK-REGISTER.md) | 18 ranked risks with mitigations; the 99% escape valve; the privacy acceptance test. |
| [BUILD-CI.md](BUILD-CI.md) | Reproducible build, the lint rule that enforces a pure engine, CI gates (incl. the corpus gate), CVE policy, Dependabot, release/checksums. |

## The shape in one paragraph

A **pure TypeScript engine** (no Office.js, gated in Node by the existing corpus
harness) does the Bijoy→Unicode text work behind a small frozen contract. A thin
**Word host adapter** is the only code that touches Office.js: it scans body +
tables, reads fonts **per run** (so mixed-font paragraphs aren't dropped),
surfaces unsupported fonts and out-of-scope regions **loudly**, shows a real
**preview**, applies changes after taking a **snapshot**, and offers a reliable
snapshot-based **"Revert Mukti changes"**. A **bilingual, accessible** taskpane
drives it. The build is **reproducible**, the engine's purity is
**lint-enforced**, and the **≥99% corpus gate** plus CVE/secret gates block
release.

## Design rules carried in from Phase 0/1 (non-negotiable)

- NFC output; idempotent (no-op on non-Bijoy text); whitespace preserved.
- Unknown fonts → loud "unsupported", never converted. No fuzzy font matching.
- Out-of-scope regions reported as "not scanned", never silently dropped.
- Real preview before any mutation; snapshot-based revert; honest Ctrl+Z story.
- Online-first privacy claim (no document-content egress; no telemetry), with a
  network-monitor acceptance test.
- Performance budgeted per `context.sync()`, not per word.

## Still open / pending into later phases

- Spikes **A** (per-run fonts) and **C** (revert fidelity) — kits with the
  maintainer; results may adjust the host design before Phase 4 build.
- **D-0006** hosting domain name (manifest is parameterized so this isn't
  blocking the design).
- Headers/footers scope — confirmed by spike; reported as "pending" until then.

**Next gate:** Phase 3 — adversarial review of this design (≤3 rounds; ends only
when blockers are closed or explicitly accepted).
