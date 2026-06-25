# Mukti — Roadmap & upgrade plan

Where Mukti is, where it's going, and how upgrades reach users. Plain language.
This is a *plan*, not a promise of dates — it sets direction and order. Scope
decisions are recorded in [`docs/DECISION-LOG.md`](docs/DECISION-LOG.md); current
gaps are in [`docs/KNOWN-LIMITATIONS.md`](docs/KNOWN-LIMITATIONS.md).

## Where we are now — v0.1.0 (public beta)

The rebuilt Mukti is **built, verified, published, and packaged**:
- Pure-TypeScript engine, **177** mapping entries, gated by a frozen **141-case**
  corpus at **100%** character & word accuracy (idempotent, NFC, fuzz-clean).
- Word integration (scan → preview → apply → snapshot revert), bilingual
  accessible task pane, Word-only manifest, bundled OFL font, CI + auto-deploy.
- **Spike D (the encoding seam) is confirmed in real Word.** ✅

It's a **beta** because two in-Word checks and the public hosting step remain.

## The path to v1.0 (stable) — the immediate plan

These are the only things between the beta and a finished, installable v1.0.
They are tracked in [`docs/RELEASE-CHECKLIST.md`](docs/RELEASE-CHECKLIST.md).

| Step | Who | Status |
|---|---|---|
| Spike D — encoding seam | maintainer | ✅ confirmed in Word |
| Spike A — per-word font reading (self-contained kit) | maintainer | ⏳ run in Word |
| Spike C — snapshot/revert round-trip (self-contained kit) | maintainer | ⏳ run in Word |
| Turn on GitHub Pages hosting | maintainer | ⏳ `RUNBOOK.md` §2 |
| Choose + point a custom domain | maintainer | ⏳ `RUNBOOK.md` §3 |
| Replace placeholder icons with a designed mark | needs a designer/tool | ⏳ nice-to-have |
| End-to-end test on a real `.docx` + privacy network test | maintainer | ⏳ checklist |
| Tag the release → auto-deploy | maintainer (1 click) | ⏳ |

When those are green, v1.0 ships on the custom domain and anyone can install it
via [`INSTALL.md`](INSTALL.md).

## v1.1 — Fast-follow (the next improvements)

The spec's planned fast-follow set, in rough priority order:
1. **Selection-only conversion** — convert just the highlighted text, not the
   whole document.
2. **"Scan document" with highlighting** — find and visually flag legacy text
   before converting.
3. **Diff view** — a clearer before/after comparison.
4. **Remembered preferences** — language choice and options persist.
5. **Broader font + glyph coverage** — keep widening the supported-font list and
   the conjunct map (continuing the work already begun: 94 → 177 entries), plus
   the **ZWJ `র‍্য`** case (e.g. র‍্যাব) noted in known-limitations.
6. **Headers/footers conversion** — once the spike confirms the approach (today
   they're reported as "pending", not converted).
7. **Sample gallery** — ready-made before/after examples.

## v2.0 and later (bigger, experimental)

- **Reverse conversion (Unicode → Bijoy)** — experimental and lossy by nature; it
  will itemise what can't be converted and report a deterministic **coverage
  ratio** (never a vague "confidence"), per the project's honesty rule.
- **Per-font target picker** — choose the output font.
- **Unknown-font mapping assistant** — help map a legacy font Mukti doesn't yet
  know.
- **Conversion report** — a summary of what was converted, skipped, and flagged.

## Deliberately NOT planned (cut)

- **Runtime, community-pluggable conversion profiles.** Mapping profiles stay as
  static, maintainer-edited, schema-validated JSON (regex-DoS-sanitised). This
  keeps the tool safe and trustworthy; community contributions come via pull
  requests to that data, reviewed and gated by the corpus.

## How upgrades reach users

- **Code/engine/data updates** (e.g. broader font coverage, bug fixes) deploy to
  the hosting automatically when a release is tagged — **users get them with no
  action**, because the add-in loads its code at launch from the fixed custom
  domain.
- **Manifest changes** (new buttons, a new permission, a new Id or domain) are the
  *only* changes that require users to **re-install (re-sideload)** the add-in. We
  avoid these where possible and always announce them in the release notes.
- Every change is gated by the same checks: the ≥99% corpus gate, lint, the
  engine-purity rule, tests, and the security/CVE scans.

## How to propose or track changes

Open an issue (templates provided) or a pull request — see
[`CONTRIBUTING.md`](CONTRIBUTING.md). Significant decisions are logged in
[`docs/DECISION-LOG.md`](docs/DECISION-LOG.md); a future maintainer/developer
starts with [`MIGRATION.md`](MIGRATION.md).
