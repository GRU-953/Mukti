# Phase 2 — Architecture & frozen interfaces

This is the design Mukti is built against. The interfaces here are **frozen
contracts**: Phase 4 implements behind them; parallel work cannot drift because
the boundaries are fixed. Nothing here is production logic — these are the seams.

## Layers (strict dependency direction)

```
        ┌─────────────────────────────────────────────┐
        │  taskpane UI  (HTML/CSS/TS, bilingual, a11y)  │   src/taskpane/
        └───────────────┬───────────────────────────────┘
                        │ calls (typed)
        ┌───────────────▼───────────────────────────────┐
        │  host adapter  (Office.js / Word)              │   src/host/
        │  scan · preview · apply · revert · detect      │
        └───────┬───────────────────────┬────────────────┘
                │ uses                   │ uses
   ┌────────────▼─────────┐   ┌──────────▼───────────────┐
   │  ENGINE (pure TS)    │   │  font registry (pure TS) │   src/engine/
   │  Bijoy→Unicode       │   │  known-font policy       │
   │  ZERO Office.js      │   └──────────┬───────────────┘
   └────────────┬─────────┘              │ reads
                │ reads                   │
        ┌───────▼──────────────────────────▼─────────────┐
        │  DATA (static JSON, schema-validated)           │   data/
        │  mapping tables · known-font list               │
        └─────────────────────────────────────────────────┘
```

**The one rule that is lint-enforced:** `src/engine/**` and `src/host` may
**never import `office-js`** from the engine. The engine runs under Node and is
gated by the corpus harness without Word. Office.js is only touched in
`src/host/**` and the taskpane. (See `BUILD-CI.md` for the lint rule.)

### Why this shape

- It is the inverse of the prior versions, where conversion was entangled with
  Office.js across `word-processor.js`/`taskpane.js` (do-not-repeat M1, H5, H6).
- The engine being pure is what let us run Spike B and Phase-1 validation in
  Node. The same harness gates the real engine in CI.
- Office.js, host I/O, and undo are isolated in `src/host`, so the risky
  platform behaviour (Spikes A & C) is contained behind a small interface.

## Module responsibilities

| Module | Responsibility | May import Office.js? |
|---|---|---|
| `src/engine` | Pure text: Bijoy→Unicode, NFC, idempotency guard, URL/email protection. | **No** |
| `src/engine/font-registry` | The known-font list + classification policy (pure data + logic). | **No** |
| `data/` | Static, schema-validated JSON: mapping tables, font list. | n/a |
| `src/host` | Word interaction: scan body+tables, read fonts per run, preview, apply, snapshot/revert, scope reporting. | Yes |
| `src/taskpane` | Bilingual accessible UI; calls host; renders preview, warnings, "not scanned" report. | Yes (Office.onReady) |
| `src/commands` | Ribbon command entry points. | Yes |
| `tools/corpus` | The accuracy harness + freeze (already built). | **No** |

## Data flow — one conversion

```
[User clicks Convert]
   → host.scanDocument()                  // body + tables; reads font per run
        → ScanReport { regions[], unscanned[], unsupportedFonts[] }
   → for each region with a KNOWN Bijoy font:
        engine.convert(region.text)        // pure, NFC, idempotent
   → host builds Preview (before/after) — NO document mutation yet
   → [User reviews preview, clicks Apply]
   → host.applyConversion(plan)            // snapshot first, then mutate + set font
        → ApplyResult { changed, snapshotId }
   → [User may click "Revert Mukti changes"]
        → host.revert(snapshotId)          // restore text + formatting from snapshot
```

Key properties enforced by the design:
- **Unknown/Bangla-looking-but-unlisted fonts** are surfaced in
  `unsupportedFonts` and **left untouched** (loud, never silent — do-not-repeat
  H4).
- **Out-of-scope regions** (footnotes, text boxes, comments, fields, SmartArt)
  are returned in `unscanned` and shown to the user, **never silently dropped**
  (do-not-repeat M6). Headers/footers: pending Spike spike result.
- **Preview is real** and precedes any mutation (do-not-repeat H6).
- **Revert** restores a snapshot; we do not promise byte-identical OOXML and we
  are honest that Ctrl+Z count is Word-determined (Spike C).
- **Performance** is budgeted per `context.sync()`; the host batches reads and
  writes (do-not-repeat M3), targets set by a calibration spike in Phase 4.

## The frozen interfaces

The concrete TypeScript contracts live next to the code they govern, as
type-only files (no implementation):

- [`src/engine/contracts.ts`](../../src/engine/contracts.ts) — the pure engine +
  font-registry API.
- [`src/host/contracts.ts`](../../src/host/contracts.ts) — the Word host adapter
  API (scan / preview / apply / revert) and its data shapes.
- [`data/schema/mapping-table.schema.json`](../../data/schema/mapping-table.schema.json)
  — the static mapping-data format (schema-validated, regex-DoS-safe).

These three files are the Phase 3 adversarial-review surface and the Phase 4
build target. Changing them after Phase 3 sign-off is itself a logged decision.
