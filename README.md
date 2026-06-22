# Mukti

**Mukti** is a free, open-source Microsoft Word add-in that converts legacy
**Bijoy / SutonnyMJ** Bangla text into proper **Unicode** Bangla — with one
click, a preview before anything changes, and a reliable way to undo.

> **Status: Phase 0 (de-risking).** No production add-in code exists yet. This
> repository currently contains the planning, forensic analysis, and safety
> groundwork that must come *before* a clean rebuild. See
> [`docs/phase0/`](docs/phase0/).

## What it will do (MVP)

- Convert **Bijoy/SutonnyMJ → Unicode** for the whole document in one click.
- **Preview** the result before applying it.
- A reliable **"Revert Mukti changes"** command (not a fragile Ctrl+Z guess).
- Swap the legacy font to a single, **bundled, openly-licensed** Unicode font.
- **Fail loudly** on fonts it doesn't recognise — never silently mangle text.
- Bilingual UI (Bangla default, English toggle), keyboard-operable.

## Privacy

Your document content never leaves your device — no text, filenames, or
metadata are transmitted, and there is no telemetry. Like every Office add-in,
Mukti loads its program code from Microsoft's servers and from the project's
hosting when it starts, so it needs an internet connection to launch. Mukti is
**online-first**; we do not advertise it as "offline."

## Licence

[MIT](LICENSE). Bundled fonts are openly licensed (SIL OFL) and retain their
own licence/NOTICE files.

## Documentation

| Document | What it is |
|---|---|
| [`docs/phase0/`](docs/phase0/) | De-risking: forensic analysis of prior versions, the do-not-repeat list, reuse manifest |
| [`docs/GLOSSARY.md`](docs/GLOSSARY.md) | Plain-language glossary of every technical term used here |
| [`docs/DECISION-LOG.md`](docs/DECISION-LOG.md) | Every significant decision, why it was made, and by whom |
| [`docs/REUSE-MANIFEST.md`](docs/REUSE-MANIFEST.md) | What (if anything) we salvage from prior versions, and the verdict on each piece |
