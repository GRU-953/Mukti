# Mukti Audit Report — v2.0.9

Audit of `D:\Test_files` using `tools/AuditScanner` on 2026-06-26.

---

## Summary

| Metric | Result |
|--------|--------|
| Documents scanned | 756 |
| Files skipped (errors) | 0 |
| Office owner/lock files excluded by design | 1 (`~$...docx`, 162 bytes — not a document) |
| Files with Bijoy text | 167 |
| Largest single file | 32,423 runs |
| Safety failures | 1 file, 2 occurrences (pre-existing source data, not an engine fault) |
| Elapsed | ~35 seconds |

The Bijoy file count rose from 163 to **167** in v2.0.9: the AuditScanner now shares the production
`FontRegistry` family list (exact-match after comma-strip + whitespace-collapse), so it detects the
same fonts the add-in converts — including `SamakalMJ`, `JomunaMJ`, `SutonnyCMJ`, `SutonnySushreeMJ`,
and the newly verified `Siyam Rupali ANSI`. Confirmed-Unicode look-alikes (`SutonnyOMJ`, `NikoshMJ`,
`TangonMotaMJ`, `ArhialkhanMJ`, `SonkhoMJ`) are explicitly excluded and never converted (see D-0006).

Every real document was processed with zero read failures. The single `~$`-prefixed entry is a
transient Microsoft Office owner/lock file (written while a document is open); it has no ZIP central
directory and is never a user document, so the scanner now excludes `~$` files by design rather than
counting them as a skip.

---

## Bijoy Scan

163 of 756 processed files contain text in SutonnyMJ or Bijoy font. The largest files are:

| Runs | File |
|------|------|
| 32,423 | Draft Programme Implimentation Guideline-UPG_Urban.docx |
| 23,066 | Handout on ইন্টিগ্রেটেড কেয়ার সলিউশন for STO.docx |
| 13,653 | Participants Selection guideline for 2026 cohort_Rural, Final_Last.docx |

Several filenames appear twice in the report — these are distinct files with the same name in different subdirectories under `D:\Test_files`, processed separately (not a scanner bug).

---

## Safety Scan

The safety scan converts every Bijoy run through the engine and checks that the output contains only valid Unicode characters. Characters are valid if they are:

- Bengali Unicode block (`U+0980`–`U+09FF`)
- ASCII (codepoint < 128)
- Non-breaking space (`U+00A0`) — common in Office documents
- Bengali punctuation (`।`, `॥`)
- Greek/extended Latin range
- Smart quotes, dashes, ellipsis, horizontal bar
- Ballot box symbols (`☐`, `☑`, `☒`)

### Result: 1 file, 2 occurrences

**File:** `Handout on ইন্টিগ্রেটেড কেয়ার সলিউশন for STO.docx`

**Code point:** `U+F0FC` (Private Use Area) — appears twice

### Root cause

`U+F0FC` is a Private Use Area character used by the Wingdings font for a checkmark symbol. The document contains a text run tagged with the `SutonnyMJ` font name that actually contains a Wingdings glyph (code point 0xFC in Wingdings encoding = checkmark). Because the run is labelled SutonnyMJ, the AuditScanner processes it and the Bijoy converter passes the unrecognised codepoint through verbatim.

### Impact

The Mukti engine will produce a `U+F0FC` PUA character in the converted output for those 2 runs. This will render as a blank box or platform-dependent glyph rather than a visible Bengali character.

### Recommended fix (document owner)

Open the file in Word, find the two runs containing the Wingdings checkmark, and either:
1. Re-apply the correct `Wingdings` font to those runs so they render correctly, or
2. Replace the Wingdings character with the Unicode checkmark (`✓` U+2713 or `✔` U+2714).

No engine change is needed — this is a data quality issue in the source document.

---

## Notes

- The scanner was validated against 387 corpus test cases (see `src/Mukti.Engine.Tests/`).
- All U+00A0 (non-breaking space) characters are correctly allowed; they appeared as false positives in v2.0.3 and were fixed in v2.0.4.
- The safety scan now runs correctly in `--mode safety`; prior to v2.0.4 a logic bug caused the converter to never be loaded in safety-only mode.
