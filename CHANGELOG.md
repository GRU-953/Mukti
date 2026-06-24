# Changelog

All notable changes to Mukti are documented here. Format: [Keep a Changelog];
versioning: [Semantic Versioning].

## [0.1.0] — Public beta (pre-release)

First public release of the rebuilt Mukti — a Microsoft Word add-in that converts
legacy **Bijoy / SutonnyMJ** Bangla text to **Unicode**.

### Added
- **Conversion engine** (pure TypeScript, zero Office.js): longest-match glyph
  mapping + Bengali cluster reordering (pre-base vowels, reph), guaranteed NFC
  output, idempotency guard, URL/email passthrough. **177** mapping entries.
- **Word integration**: scan (body + tables) → preview → apply → reliable
  snapshot-based **"Revert Mukti changes"**, with a TOCTOU guard and per-run font
  reading; out-of-scope regions are reported, never silently skipped.
- **Bilingual task pane** (Bangla default, English toggle), keyboard-operable,
  WCAG 2.1 AA, with a real before/after preview and loud unsupported-font handling.
- **Bundled output font**: Noto Sans Bengali (SIL OFL), self-hosted.
- **Frozen gold-standard corpus** (141 cases, visible + sealed held-out) and a
  Node test harness; the engine scores **100%** character and word accuracy,
  idempotent, NFC-stable, fuzz-clean.
- Reproducible build (pinned toolchain, `npm ci`), CI (lint + engine-purity rule
  + tests + corpus gate), Dependabot, and a tag-triggered release/deploy workflow.
- Full documentation: install guide, known limitations, runbook, release
  checklist, decision log, continuity & migration guides.

### Known / pending (see `docs/KNOWN-LIMITATIONS.md`)
- Three short in-Word checks (encoding, per-word fonts, undo fidelity) are
  pending final confirmation; authoritative research rates all three high
  confidence. Until confirmed and hosted on a custom domain, this is a **beta**.
- Scope is deliberately tight: Word only; Bijoy→Unicode only; body + tables
  (headers/footers pending; footnotes/text boxes/comments/fields/SmartArt
  reported as "not scanned").

[Keep a Changelog]: https://keepachangelog.com/en/1.1.0/
[Semantic Versioning]: https://semver.org/spec/v2.0.0.html
