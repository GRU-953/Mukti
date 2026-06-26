# Mukti Audit Report — v2.0.4

Audit of `D:\Test_files` (757 Office documents) using `tools/AuditScanner` on 2026-06-26.

---

## Summary

| Metric | Result |
|--------|--------|
| Files scanned | 756 |
| Files skipped | 1 (Word lock file `~$...docx` — not a real document) |
| Files with Bijoy text | 163 |
| Total Bijoy runs | 306,620+ |
| Safety failures | 1 file, 2 occurrences |
| Elapsed | ~30 seconds |

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
