# Mukti

**Mukti** is a free, open-source Microsoft Word add-in that converts legacy
**Bijoy / SutonnyMJ** Bangla text into proper **Unicode** Bangla — with one
click, a preview before anything changes, and a reliable way to undo.

> **Status: MVP built and verified; pre-release.** The conversion engine, the
> Word integration, the bilingual task pane and the build pipeline are all in
> place. The engine scores **100%** on a frozen 115-case test set (character and
> word accuracy, idempotency, NFC, fuzz). What remains before the **v1.0**
> release is validation *inside real Word* (three short maintainer-run test kits)
> and the release steps — see [`docs/RELEASE-CHECKLIST.md`](docs/RELEASE-CHECKLIST.md).

## What it does (MVP)

- Converts **Bijoy/SutonnyMJ → Unicode** for the whole document in one click.
- **Preview** before anything changes; reliable **"Revert Mukti changes"**.
- Swaps the legacy font to a single, **bundled, openly-licensed** Unicode font
  (Noto Sans Bengali, SIL OFL).
- **Fails loudly** on fonts it doesn't recognise — never silently mangles text;
  reports anything it didn't scan.
- Bilingual UI (Bangla default, English toggle), keyboard-operable, WCAG-AA.

Scope is deliberately tight: Word only, Bijoy→Unicode only. See
[`docs/KNOWN-LIMITATIONS.md`](docs/KNOWN-LIMITATIONS.md).

## Install

Once v1.0 is released, follow [`INSTALL.md`](INSTALL.md) — the easiest path is
Word-on-the-web → Add-ins → **Upload My Add-in**. (Until then the add-in is
pre-release.)

## Privacy

Your document content never leaves your device — no text, filenames, or
metadata are transmitted, and there is no telemetry (enforced by a strict
content-security policy and a self-hosted font; see
[`docs/phase2/SECURITY.md`](docs/phase2/SECURITY.md)). Like every Office add-in,
Mukti loads its program code from Microsoft's servers and the project's hosting
when it starts, so it needs internet to launch. Mukti is **online-first**; we do
not advertise it as "offline".

## How it's built

A pure **TypeScript engine** (zero Office.js, tested under Node and gated by the
corpus) does the conversion behind a frozen contract; a thin **Office.js host**
talks to Word; a **task pane** drives it. Engine purity is lint-enforced.

```
src/engine   the pure conversion engine (+ data/ mapping tables)
src/host     the Word/Office.js integration
src/taskpane the bilingual UI · src/commands the ribbon button
corpus/      the frozen gold-standard test set (visible + sealed held-out)
tools/       the corpus harness, data generator, web bundler
```

Build & check: `npm ci`, then `npm run build` (engine + add-in + manifest),
`npm test`, `npm run lint`, `npm run corpus:gate` (≥99% gate).

## Documentation

| Document | What it is |
|---|---|
| [`INSTALL.md`](INSTALL.md) | Plain-English install guide (3 methods) |
| [`docs/KNOWN-LIMITATIONS.md`](docs/KNOWN-LIMITATIONS.md) | Honest list of what it does and doesn't do |
| [`RUNBOOK.md`](RUNBOOK.md) | Maintainer's step-by-step operating guide |
| [`docs/RELEASE-CHECKLIST.md`](docs/RELEASE-CHECKLIST.md) | The manual gate before a release |
| [`CONTINUITY.md`](CONTINUITY.md) · [`MIGRATION.md`](MIGRATION.md) | Keeping the project alive · onboarding a future developer |
| [`docs/DECISION-LOG.md`](docs/DECISION-LOG.md) | Every significant decision and why |
| [`docs/GLOSSARY.md`](docs/GLOSSARY.md) | Plain-language glossary of every term |
| [`docs/phase0/`](docs/phase0/) … [`docs/phase3/`](docs/phase3/) | The full trail: de-risking, design, adversarial review |

## Licence

[MIT](LICENSE). The bundled font is openly licensed (SIL OFL) and retains its
own licence/NOTICE. Mapping data is the maintainer's own MIT work plus public
Unicode/Microsoft specifications.
