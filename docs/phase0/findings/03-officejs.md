# Findings 03 — Office.js / Host Integration & UX (forensic, READ-ONLY)

Scope analyzed across 5 prior versions (Mukti-2.0.0 … Mukti-main). `Mukti-main` == `Mukti-3.1.0`
(byte-identical in scope). Evolution: word-processor 290→346 lines; the only structural
changes were (a) adding headers/footers pass, (b) switching from whole-paragraph `convert()`
to a "full-if-Bijoy-font / selective word-by-word otherwise" strategy via `selectiveConvert`,
(c) adding `convertWithFont`. **Font-reading granularity never changed: paragraph-level only.**

All file:line refs below are in `Mukti-main/` unless noted.

---

## 1. How fonts are read per run/range

**It reads ONE font name per paragraph (or per selection / per Excel batch / per pptx shape) — it NEVER iterates runs.**

Word, document path — `src/office/word-processor.js:56-64`:
```js
paragraph.load("text, font/name, font/size, font/bold, font/italic, font/color, font/underline, font/strikeThrough, font/highlightColor");
await context.sync();
const text     = paragraph.text;
const fontName = paragraph.font.name;          // <-- the ONLY font read
```
- Font gate then uses this single name: `if (isB2U && !isBijoyFont(fontName)) { skipped++; return; }` (`:70-71`).
- Iterates `context.document.body.paragraphs` items (`:138-142`), NOT runs. There is **no use of `getTextRanges`, `body.search`, or run iteration anywhere in the codebase** (grep for `getTextRanges|\.search\(|getOoxml|insertOoxml|trackedChanges` → zero hits).
- Selection path identical: `sel.font.name` on `context.document.getSelection()` (`:189-198`).
- Excel reads ONE font for a 50-row batch: `batch.format.font.load("name")` then `batch.format.font.name` (`excel-processor.js:102-106`).
- PptX reads ONE font per shape textRange: `textRange.font.load("name,...")` (`pptx-processor.js:86,90`).

**Implication for SPIKE (A):** Prior code proves `paragraph.font.name` / `range.font.name` is readable and usable as a Bijoy gate **when the paragraph is font-homogeneous**. It does NOT prove per-run access. Office.js returns `null` for `range.font.name` on a mixed-font range — a paragraph with "Hello SutonnyMJ-text" in two fonts yields `null`, which `isBijoyFont(null)` rejects → that paragraph is silently skipped. So per-run font detection is **UNPROVEN** in this codebase; the new spike must add it (paragraph.getRange().split / search-based ranges) — prior art gives no working example.

## 2. WordApi requirement set per key call (target = 1.3)

**The manifest declares NO `<Requirements><Sets>` at all** (`manifest.xml`, `manifest-production-template.xml` — grep `Requirements|<Set ` → none). So nothing is enforced today; the host loads regardless. Against the new 1.3 target, the API calls actually used:

| Call | File:line | Min set | >1.3? |
|---|---|---|---|
| `document.body.paragraphs` + `.load("items")` | wp:138 | 1.1 | no |
| `paragraph.font.name/size/bold/...` (subprops) | wp:57-64 | 1.1 | no |
| `paragraph.getRange()` | wp:77 | 1.1 | no |
| `range.insertText(text,"Replace")` | wp:116 | 1.1 | no |
| `range.font.name = ...`, `font.highlightColor` | wp:117-119,319 | highlightColor 1.1; OK | no |
| `body.tables`, `table.rows`, `row.cells`, `cell.body` | wp:237-252 | 1.3 | **exactly 1.3** |
| `document.sections` + `section.getHeader/getFooter("Primary")` | wp:268-285 | **1.1 (getHeader) but `sections` collection load = 1.1**; `getHeader` 1.1 | no |
| `document.getSelection()` | wp:189 | 1.1 | no |
| `body.load("text")` | wp:337 | 1.1 | no |

**Verdict: nothing exceeds 1.3.** Tables (`cell.body.paragraphs`) are the highest, landing exactly at 1.3 — so the "body+tables" scope is the binding constraint that forces 1.3 (body-only would be 1.1). No search options, no `body.font`, no track/undo APIs, no `getTextRanges` are used, so none of those gate anything. **Action: the new manifest MUST add `<Requirements><Sets DefaultMinVersion="1.1"><Set Name="WordApi" MinVersion="1.3"/>` — it is currently missing entirely.**

## 3. How conversions are APPLIED + output font

- Word: `const newRange = range.insertText(converted, "Replace"); newRange.font.name = isB2U ? TARGET_UNICODE_FONT : TARGET_BIJOY_FONT;` (`wp:116-117`). Full-paragraph string replace.
- Then re-applies saved props (`applyFontProps`, `wp:312-320`): size/bold/italic/color/underline/strikeThrough/highlightColor wrapped in try/catch.
- Output font = **`TARGET_UNICODE_FONT = "Kohinoor Bangla"`** for B2U (`wp:24`); U2B target `"SutonnyMJ"`. Same constants in excel/pptx/commands.
- Excel: writes back the whole `batch.values` 2D array + `batch.format.font.name` (`excel-processor.js:158-159`). PptX: `textRange.text = converted` + `textRange.font.name` (`pptx-processor.js:136-137`).
- **No cluster reordering at apply time** — apply is a dumb string swap; reordering (if any) lives in `core/converter` (out of my scope). Office layer just does `insertText`. So SPIKE (B) correctness is NOT exercised at the host layer; insertText("Replace") is proven to work for replacing paragraph text, but mixed-font paragraphs lose per-run fonts (entire new range gets ONE font name).

## 4. context.sync() usage — performance

Per Word document conversion, sync() count is **roughly 1 + (paragraphs) + (batches) + table-overhead**, which is the main smell:
- Body: per paragraph there are **TWO syncs inside `convertParagraph`** (`wp:61` load text/font, `wp:82` reload range font) **plus a third** (`wp:119`) after insertText → **3 syncs per converted paragraph**, executed serially. Then one batch-boundary sync every 30 paragraphs (`wp:156`). The 30-para batching is illusory: each paragraph already awaits 3 syncs sequentially before the batch sync, so a 1000-paragraph doc ≈ 3000+ round-trips.
- Tables are worse: a sync per table, per row, per cell, **plus 3 per cell-paragraph** (`wp:239,244,249,254` then convertParagraph's 3). Deeply nested, fully serial.
- Excel is the sane one: batches 50 rows × all cols, ONE sync to load + ONE to write per batch (`excel-processor.js:101,161`). Good model to emulate.
- PptX: ~2 syncs per shape (load + write), serial across all shapes/slides.

**Smell: Word is per-element, not per-sync-budgeted.** Loads are not coalesced (could load all paragraph fonts in one sync, decide in JS, then issue all insertText + one final sync). The redundant `range.getRange()` reload (`wp:77-82`) duplicates the font already loaded on `paragraph` (`wp:57`) — pure waste, one extra sync per paragraph. New plan's "budget per sync()" goal requires a full rewrite of the Word loop toward the Excel batch model.

## 5. UNDO / REVERT — unreliable

**There is NO snapshot/restore and NO use of Word's native undo. "Undo" = destructive reverse re-conversion of the WHOLE document.** `taskpane.js:487-498`:
```js
function onUndo() {
  setDirection(currentDirection === B2U ? U2B : B2U);
  onConvertAll();                 // re-runs conversion in the opposite direction
  document.getElementById("btn-undo").disabled = true;
}
```
- `let lastConversionState = null; // For undo` (`taskpane.js:40`) is **declared and NEVER read or written** — dead code; the intended snapshot was never implemented.
- It does NOT call `context.document.undo()` or rely on Ctrl+Z; it issues a fresh forward conversion in reverse. This is **lossy and non-idempotent**: Bijoy→Unicode→Bijoy round-trips through different converters/repair/learned-corrections, will not restore the original bytes, re-stamps fonts to "SutonnyMJ", and re-runs over already-correct English/Unicode text. After undo the button is disabled (no redo, single level).
- Word's native undo stack: each `insertText` is itself undoable via Ctrl+Z, but there is no in-add-in "Revert Mukti changes" tied to it.

**Implication for SPIKE (C):** Nothing reusable. The new "reliable Revert" must be built from scratch. Prior code proves only that `insertText("Replace")` lands on Word's native undo stack (so Ctrl+Z works per edit). A reliable revert needs either (a) a captured before-text/before-font snapshot keyed by range, or (b) a tracked-changes / single grouped undo entry — none of which exists here.

## 6. Preview — none

**There is no preview-before-apply.** Grep `preview|Preview` → zero hits in taskpane.js/html/office. `onConvertAll` (`taskpane.js:304`) calls the processor which mutates the document immediately (`insertText` inside the same run). The only "non-destructive" surface is the **Quick Text Converter** tab (`onConvertText`, `:410-431`) — a standalone textarea→textarea that converts a pasted string and shows output; it does not touch the document and is not a document preview. SPIKE: real preview is greenfield.

## 7. Scope: tables / headers / footers / footnotes

- **Body paragraphs:** yes (`wp:138-161`).
- **Tables:** yes — nested rows/cells/cell-paragraphs (`processWordTables`, `wp:235-264`). (This is what forces WordApi 1.3.)
- **Headers/footers:** yes but **only `"Primary"` type** (`wp:266-296`); first-page and even-page headers/footers are NOT handled (`for (const type of ["Primary"])` — array has a single element). New spec lists headers/footers as "pending" — prior impl is partial and silently swallows all errors (`catch (_e) { /* skip */ }`).
- **Footnotes / textboxes / comments / fields / SmartArt:** NOT handled (correctly OUT per spec).
- **Reporting of unscanned regions:** **none.** Skips are silent (`catch (_e) {}` everywhere in tables/headers/footers; non-Primary headers, mixed-font paragraphs, and footnotes are dropped with no user-visible "N regions not scanned" message). The results UI only reports converted/skipped/errors counts (`taskpane.js:285-295`), where "skipped" conflates "wrong font", "no convertible content", and "couldn't read" — a transparency gap.

## 8. REUSE VERDICT per file

| File | Verdict | Why |
|---|---|---|
| `office/word-processor.js` | **REWRITE** | Right idea (paragraph font gate, insertText), wrong execution: 3 syncs/para, redundant range reload, paragraph-level font (no per-run), no preview, no real undo, silent skips. Salvage the `extractFontProps`/`applyFontProps` pattern and the font-gate logic; rebuild the loop on a load-all-then-batch-write model. |
| `office/excel-processor.js` | **ADAPT** | Batch model (50 rows, 2 syncs/batch) is the sane template to copy; keep it. Still paragraph/batch-level font only and no undo/preview. |
| `office/pptx-processor.js` | **ADAPT** | Out of new scope (Word-first) but structurally clean; ~2 syncs/shape. Park it. |
| `taskpane.js` (office-integration parts: onConvertAll/Selection/onUndo) | **REWRITE** | Orchestration is fine; onUndo is dangerous (destructive reverse), no preview, `lastConversionState` dead. Keep UI scaffolding (tabs/progress/results), rebuild convert/undo flow. |
| `commands/commands.js` | **ADAPT→REWRITE** | Ribbon quick-convert works and correctly calls `event.completed()`; but uses content heuristics (`hasBijoyText`) to auto-pick direction and has the same single-font, no-undo limits. Reuse the `Office.actions.associate` + `event.completed()` plumbing; redo conversion body. |
| `taskpane.html` / `commands.html` | **ADOPT (scaffold)** | Keep as UI shell; add Preview + "Revert Mukti changes" controls (current `btn-undo` is a danger button wired to reverse-convert). |

## 9. Concrete inputs for the 3 spikes

**(A) Per-run font access** — PROVEN: `range.font.name` / `paragraph.font.name` returns a usable name on homogeneous ranges (used as the gate everywhere). UNPROVEN/MISSING: any per-run read; mixed-font paragraphs yield `null` and are silently skipped (`isBijoyFont(null)` false). The spike must introduce sub-paragraph ranges (search-based ranges or split) — there is zero prior code to copy, only the gate predicate `isBijoyFont(fontName)` (`detector.js`).

**(B) Cluster reordering at apply** — Host layer only does `range.insertText(converted, "Replace")` then stamps `font.name = "Kohinoor Bangla"`. PROVEN: string-level replace + font stamp survives and re-applies bold/italic/size/color. NOT exercised: reordering correctness is entirely inside `core/converter` (out of scope here); the Office layer is reorder-agnostic. Risk: insertText replaces the WHOLE range with one font, so any intra-paragraph font/format variation is flattened.

**(C) Undo fidelity** — PROVEN: each `insertText("Replace")` is individually on Word's native undo stack (Ctrl+Z works per edit). UNPROVEN/ABSENT: any grouped/reliable revert, any snapshot. `onUndo`'s reverse-conversion is NOT byte-faithful and must not be reused. New revert = capture before-snapshot (text+font per touched range) or use a single undo group; build fresh.

## 10. TOP RISKS — do not repeat

1. **Mixed-font paragraphs silently dropped.** `paragraph.font.name` is `null` when a paragraph spans fonts → gate rejects → real Bijoy text skipped with no report. Per-run detection is mandatory, not optional.
2. **"Undo" is destructive reverse-conversion** (`taskpane.js:488`). It re-converts the entire doc the other way (lossy, re-fonts, re-touches English). Ship a snapshot/grouped-undo revert; never reuse `onUndo`.
3. **3 context.sync() per paragraph, fully serial** (`wp:61,82,119`), plus a wasted duplicate font reload (`wp:77-82`). 1000-para doc ≈ 3000+ round-trips. Rewrite to load-all → decide-in-JS → batch-write (Excel model at `excel-processor.js:101,161` is the template).
4. **Manifest declares no requirement set at all.** Add `WordApi MinVersion 1.3` (tables force 1.3; body-only would be 1.1) so unsupported hosts fail gracefully instead of erroring mid-run.
5. **Silent skips / no transparency.** Tables, non-Primary headers, footnotes, and unreadable ranges are swallowed by bare `catch (_e) {}`; "skipped" count conflates 3+ reasons. New UI must report unscanned regions (headers/footers pending, footnotes OUT) explicitly.
6. **No preview.** Conversion mutates immediately; the only safety net is destructive undo. Preview-before-apply is greenfield and must precede the apply pass.
7. **Whole-paragraph insertText flattens intra-paragraph formatting** — the new range gets a single font/format; per-run preservation needs sub-paragraph application, not one `insertText` per paragraph.
8. **Headers/footers only "Primary"** (`wp:273`) — first/even-page variants missed. Matches "pending" status but don't mistake current code for complete.
