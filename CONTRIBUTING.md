# Contributing to Mukti

Thanks for your interest! Mukti is a free, MIT-licensed tool to make legacy
Bangla universal. Contributions are welcome — code, mapping data, documentation,
translations, or testing in real Word.

## Ground rules (what keeps Mukti trustworthy)

- **The corpus gate is law.** Every change must keep `npm run corpus:gate` at
  ≥99% character *and* word accuracy on the visible **and** held-out sets, with
  zero idempotency / NFC / fuzz failures. Do **not** read or tune against
  `corpus/heldout/` — it is the sealed slice that keeps the score honest.
- **The engine stays pure.** `src/engine/**` must never import Office.js or any
  DOM/browser global — this is lint-enforced. All Word-specific code lives in
  `src/host/`.
- **No secrets, ever.** Keys, certificates and `.env` files are git-ignored and
  blocked by push protection. Development certificates are generated locally.
- **Output is always NFC**, whitespace is preserved, and unknown fonts fail
  loudly (never silently mangle text).

## Getting set up

```bash
npm ci          # install exact pinned dependencies
npm test        # unit tests + data-schema validation
npm run lint    # style + engine-purity rule + type-check
npm run corpus:gate   # the accuracy gate (build engine + run the corpus)
npm run build   # engine + task-pane bundle + manifest
```

Node version is pinned in `.nvmrc`.

## Adding to the conversion map

The mapping data lives in `data/bijoy-sutonnymj.json` (schema:
`data/schema/mapping-table.schema.json`) — code-point arrays → NFC targets, no
regex. After editing, run `npm run generate` then `npm run corpus:gate`. Add a
real-word corpus case that exercises the new entry, with a verified expected
value. A font that cannot reach 99% may still ship under the documented escape
valve (see `docs/DECISION-LOG.md`).

## How decisions are made

Significant choices are recorded in `docs/DECISION-LOG.md`. The design rationale
and the adversarial review are under `docs/`. New contributors: start with
[`MIGRATION.md`](MIGRATION.md).

## Reporting problems

Open an issue (templates provided). For security or privacy concerns, see
[`SECURITY.md`](SECURITY.md).
