# Spikes A & C — documentary evidence (Word JavaScript API, target WordApi 1.3)

Authoritative desk research against Microsoft Learn. This complements the in-Word
Script Lab kits in `spike-A-per-run-font.md` and `spike-C-undo-revert.md`: those
prove behaviour empirically; this records what the published contract guarantees.
All quotations are from Microsoft Learn (en-us). Researched 2026-06-24.

---

## SPIKE A — finest granularity for reading `font.name` within a paragraph

### Answer

At WordApi 1.3 the finest **decomposition** primitive is a delimiter-/ending-mark
split (`Range.getTextRanges` or `Range.split`), and the finest **read** unit is any
sub-`Range` you can address — but there is **no API that splits on font (run)
boundaries**. The realistic approach is: split a paragraph into the smallest
text units the API offers (typically per word, via `getTextRanges([" "], …)`,
optionally also on punctuation), then read `font.name` on each unit. A unit whose
text spans more than one font returns `font.name === null` (the documented
"different values" sentinel). So homogeneous-font runs **cannot be reconstructed
exactly** at 1.3; the practical floor is "per word (or per delimiter token), with
null reported as mixed".

### Key evidence

1. **`getTextRanges` is in WordApi 1.3 and splits on ending marks, not fonts.**
   Signature `getTextRanges(endingMarks: string[], trimSpacing?: boolean)`. The
   `endingMarks` parameter is *"The punctuation marks and other ending marks as an
   array of strings."* and `trimSpacing` *"Indicates whether to trim spacing
   characters (spaces, tabs, column breaks, and paragraph end marks) from the
   start and end of the ranges returned in the range collection. Default is
   `false` …"*. Remarks: *"[API set: WordApi 1.3]"*.
   <https://learn.microsoft.com/en-us/javascript/api/word/word.range?view=word-js-1.3>
   Confirmed present in the 1.3 requirement-set listing for `Range`, `Paragraph`
   and `ContentControl`.
   <https://learn.microsoft.com/en-us/javascript/api/requirement-sets/word/word-api-1-3-requirement-set>

2. **`Range.split` is also WordApi 1.3** —
   `split(delimiters: string[], multiParagraphs?: boolean, trimDelimiters?:
   boolean, trimSpacing?: boolean)`, *"Splits the range into child ranges by using
   delimiters."* The `multiParagraphs` default `false` *"indicates that the
   paragraph boundaries are also used as delimiters."* Remarks: *"[API set:
   WordApi 1.3]"*. Both methods split on **literal string delimiters / ending
   marks** (and, for `split`, paragraph ends) — neither offers any "split where the
   font changes" mode.
   <https://learn.microsoft.com/en-us/javascript/api/word/word.range?view=word-js-1.3>

3. **A range spanning mixed values returns `null` for that font property.**
   *"`null` … is used to represent default values or no formatting. Formatting
   properties such as color will contain `null` values in the response when
   different values exist in the specified range. … If all text in the range has
   the same font color, `range.font.color` specifies that color. If multiple font
   colors are present within the range, `range.font.color` is `null`."* The
   documentation states the rule for `font.color`; `font.name` is the same
   `Word.Font` value-typed property and follows the identical "different values →
   `null`" contract (this is the residual point to confirm empirically — see
   below). Note this is the property-level `null` convention, distinct from the
   enum-typed `"Mixed"` value used for properties like border width.
   <https://learn.microsoft.com/en-us/office/dev/add-ins/word/word-add-ins-troubleshooting>
   (sections "Meaning of null property values in the response" and "Can't use
   Mixed to set a property")

4. **`Range.font` itself is WordApi 1.1**, so reading `font.name` on any sub-range
   produced by the 1.3 split is available: *"font … Gets the text format of the
   range. … [API set: WordApi 1.1]"*.
   <https://learn.microsoft.com/en-us/javascript/api/word/word.range?view=word-js-1.3>

### Realistic approach and its limits

- Split each paragraph with `getTextRanges` on space (and optionally on common
  punctuation, e.g. `[" ", "।", ".", ",", "?", "!"]`) → read `font.name` per token.
- A token returning a concrete name is homogeneous → safe to act on. A token
  returning `null` spans >1 font and must be **reported / skipped, not silently
  acted on** — this is exactly the "null-font → report" rule in the host-contract.
- Hard floor: a single word in two fonts (e.g. a Bijoy stem + a Latin suffix with
  no space between) is **indivisible** by these APIs, because there is no
  intra-word, font-boundary split. Such a word reads `null` and is reported. This
  is a genuine limit of 1.3, not an implementation choice.

### Residual uncertainty

- The `null`-on-mixed rule is documented explicitly for `font.color`; the same
  page does not enumerate every property. It is near-certain `font.name` behaves
  identically, but this is exactly what the Spike A Script Lab kit verifies
  in-product (it reads `font.name` per token and counts distinct values).
- Whether `getTextRanges([" "])` yields strictly per-word ranges across all hosts
  (web/desktop/Mac) and how it treats consecutive delimiters / leading-trailing
  spacing is best confirmed by running the kit.

### Confidence: **High** that per-token (≈per-word) `font.name` reading is the
finest 1.3 granularity and that mixed ranges read `null`; **Medium-High** that
`font.name` specifically returns `null` (vs `""`) on mixed runs — pending the
in-Word kit.

---

## SPIKE C — programmatic undo, and snapshot storage (settings vs CustomXML)

### (a) Does WordApi 1.3 expose programmatic undo / undo-grouping? **No.**

- The WordApi 1.3 view of `Word.Document` lists properties `body`,
  `contentControls`, `context`, `properties`, `saved`, `sections` and methods
  `getSelection`, `load`, `save`, `set`, `toJSON`, `track`, `untrack`. There is
  **no `undo`, `redo`, or undo-grouping member.**
  <https://learn.microsoft.com/en-us/javascript/api/word/word.document?view=word-js-1.3>
- Undo **grouping** is an **Excel** JavaScript API feature only, exposed via the
  `mergeUndoGroup` option to `Excel.run`: *"The Excel JavaScript API also supports
  undo grouping. This allows you to group multiple API calls into a single
  undoable action … This is done with the `mergeUndoGroup` property provided to
  the `Excel.run` function."* `Word.run` has no equivalent.
  <https://learn.microsoft.com/en-us/office/dev/add-ins/excel/excel-add-ins-undo-capabilities>
- (Edits made through the Word JS API do land on Word's native Ctrl+Z stack, but
  the number of undo steps per logical operation is host-dependent and not
  controllable — hence a dedicated snapshot→restore "Revert" is required rather
  than relying on Ctrl+Z. The `doc.undo()` seen in some Microsoft Q&A threads is
  the legacy VBA/automation object model, not the Office.js Word API.)

**Conclusion: a snapshot-restore mechanism is required.** Confirmed.

### (b) Settings bag vs CustomXML part for a pre-conversion snapshot

> Critical requirement-set finding: **`Word.Document.customXmlParts`,
> `Word.CustomXmlPart`, `CustomXmlPartCollection`, and the Word-specific
> `Word.Document.settings` / `Word.SettingCollection` were all introduced in
> WordApi 1.4 — NOT 1.3.** They appear in the 1.4 requirement-set list under the
> banner *"WordApi 1.4 added support for bookmarks, change tracking, comments,
> custom XML parts, fields, and merging and splitting table cells."* and are
> **absent** from the 1.3 list and the 1.3 `Word.Document` view.
> <https://learn.microsoft.com/en-us/javascript/api/requirement-sets/word/word-api-1-4-requirement-set>
> <https://learn.microsoft.com/en-us/javascript/api/word/word.document?view=word-js-1.3>

Implication for a strict 1.3 target:

- The **Common API settings bag** — `Office.context.document.settings`
  (`Office.Settings`) — **is** available, because it is part of the cross-host
  **Settings** requirement set, not WordApi. Its reference page lists `word-js-1.3`
  among supported monikers. *"Represents custom settings for a task pane or
  content add-in that are stored in the host document as name/value pairs. …
  saved per add-in and per document. … The name of a setting is a string, while
  the value can be a string, number, boolean, null, object, or array."*
  <https://learn.microsoft.com/en-us/javascript/api/office/office.settings>
- The **Common API custom XML part** — `Office.context.document.customXmlParts`
  (`Office.CustomXmlParts` / `addAsync`) — is likewise a Common API and is the
  cross-host route to CustomXML when the WordApi 1.4 `Word.Document.customXmlParts`
  is unavailable. (The Word-specific 1.4 surface is richer — XPath query/update —
  but is gated at 1.4.)

Is CustomXML the right place for a *potentially large* snapshot? **Yes, in
preference to the settings bag for anything sizeable**, and it is the documented
mechanism for structured per-document data:

- Microsoft's own guidance: *"Use a custom XML part to store information that has a
  structured character or when you need the data to be accessible across instances
  of your add-in."* CustomXML *"persists with the file, independent of the
  add-in."* The recommended pattern is to store the part's GUID `id` **in a
  setting** and the bulk XML in the part.
  <https://learn.microsoft.com/en-us/office/dev/add-ins/develop/persisting-add-in-state-and-settings>
- Settings, by contrast, are a JSON property bag *"managed entirely in memory"*
  during the session and serialised to the document on save — fine for small
  state (keys, the CustomXML id, user preferences) but not the natural home for a
  large text+formatting snapshot.

Limits / persistence semantics:

- **Neither** the `Office.Settings` page, the `Word.CustomXmlPart` page, nor the
  resource-limits page states an explicit byte cap for the settings bag or for a
  custom XML part. The `Word.CustomXmlPart` page documents `getXml`/`setXml` with
  **no stated size limit**.
  <https://learn.microsoft.com/en-us/javascript/api/word/word.customxmlpart>
- The one **numeric** transport limit documented is the request/response payload
  cap, stated for Excel on the web: *"Excel on the web has a payload size limit
  for requests and responses of 5MB."* It is published under Excel, so treat 5MB
  as an *indicative* per-call ceiling for the add-in↔host channel rather than a
  guaranteed Word figure. Practical takeaway: if a snapshot could approach
  megabytes, chunk it across multiple `context.sync()`/`setXml` calls and test on
  the target hosts.
  <https://learn.microsoft.com/en-us/office/dev/add-ins/concepts/resource-limits-and-performance-optimization>
- Both CustomXML parts and settings **persist inside the .docx** (Open XML),
  independent of the add-in, and survive document close/reopen — appropriate for a
  pre-conversion snapshot that must outlive the task-pane session.

### (c) When does `settings.saveAsync` persist? **Two-stage; only fully on document save.**

- *"The `get`, `set`, and `remove` methods operate only on the in-memory copy of
  the settings property bag. If your add-in is closed without calling
  `saveAsync`, the changes to settings during that session are lost."*
- *"**Note**: The `saveAsync` method persists the in-memory settings property bag
  into the document file. However, the changes to the document file itself are
  saved only when the user (or AutoRecover setting) saves the document to the file
  system."*
  <https://learn.microsoft.com/en-us/javascript/api/office/office.settings>
  <https://learn.microsoft.com/en-us/office/dev/add-ins/develop/persisting-add-in-state-and-settings>

So: `set/remove` → memory only; `saveAsync` → writes into the in-memory document;
the bytes only reach disk when the user/AutoRecover saves the file. A snapshot the
user must be able to rely on after reopening therefore needs `saveAsync`
**and** a document save (or the add-in must accept that an unsaved-then-closed
document loses the snapshot — the same caveat applies to CustomXML parts written
in-session).

### Residual uncertainty

- Exact byte limits for settings and CustomXML in Word are **undocumented**; the
  5MB figure is an Excel-on-the-web payload limit reused as a working assumption.
  Needs an in-Word stress test if snapshots can be large (long documents).
- Whether `Office.context.document.customXmlParts` (Common API) is fully
  functional on every 1.3-capable host (notably Word on the web vs desktop) is
  worth a quick in-product check, since the richer typed surface is 1.4.

### Confidence:
- (a) No programmatic undo / undo-grouping at 1.3: **Very High** (direct from the
  1.3 class view and the Excel-only grouping doc).
- (b) CustomXML is the right home for a large snapshot; **but the WordApi-typed
  `Document.customXmlParts`/`settings` are 1.4, so a strict-1.3 build must use the
  Common API `Office.context.document.{settings,customXmlParts}`**: **High.**
- (c) `saveAsync` persists into the file only on document save: **Very High**
  (verbatim from the API reference).

---

## Verdict on the current host-contract design

The design — **per-run font via `getTextRanges`, with `null` `font.name` →
report; CustomXML snapshot; no reliance on programmatic undo** — is **sound in
principle and well supported by the documentation**, with three caveats to settle
before/while building:

1. **Granularity honesty.** "Per-run" is really "per delimiter token (≈per word)".
   True per-run (font-boundary) decomposition is not possible at 1.3; sub-word
   mixed-font tokens are indivisible and will read `null`. The "null → report"
   rule correctly handles this — keep the messaging accurate ("per word, mixed
   reported") rather than implying glyph-level runs.
2. **Requirement-set correction (important).** `Word.Document.customXmlParts` and
   `Word.Document.settings` are **WordApi 1.4**. A genuinely 1.3-targeted add-in
   must store the snapshot via the **Common API** `Office.context.document`
   surface (`customXmlParts.addAsync` / `settings`), or else raise the manifest
   requirement to WordApi 1.4. Either choice is fine — but the current wording
   "`Word.Document.customXmlParts`" implies a 1.4 dependency that the stated 1.3
   target does not provide.
3. **Persistence + size.** Snapshots persist in the .docx but only reach disk on
   document save; and no documented hard size cap exists (assume ≈5MB per call,
   chunk if larger). Validate large-document snapshots in-Word.

All three are confirmable with the existing Script Lab kits plus a small
CustomXML/settings round-trip test on the target hosts. Pending that in-Word
confirmation, proceed.
