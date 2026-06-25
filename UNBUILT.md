# UNBUILT.md — Deliberate omissions in Mukti v2

Read before adding any of these. Each was explicitly considered and rejected.

---

## U-001 — AppSource / Microsoft Store distribution
**Not built.** Mukti is not published to Microsoft AppSource.
**Why:** AppSource requires paid Microsoft Partner Center membership. Unsustainable for a volunteer project.
**Path forward:** GitHub Releases direct download.

## U-002 — Automatic in-app updates
**Not built.** No auto-update mechanism.
**Why:** Auto-update requires signing certificate continuity and a release API. Re-downloading from GitHub is the v2 update path.
**Path forward:** v2.1 can add a "new version available" banner linking to GitHub Releases.

## U-003 — Reverse conversion (Unicode to Bijoy)
**Not built.** Converting Unicode back to Bijoy is not implemented.
**Why:** Reverse mapping is inherently lossy. The original Mukti reverse converter caused silent data loss (D-0001 do-not-repeat H6). Undo button is the safe alternative.
**Path forward:** Separate tool, separate corpus, separate decision.

## U-004 — Headers, footers, footnotes in Word
**Built in v2.0.1.** Windows (COM): scans and converts doc.Sections headers/footers and doc.Footnotes/Endnotes. Mac: scans via Word.js `section.getHeader/getFooter` with graceful fallback for older requirement sets.

## U-005 — Excel formula conversion
**Built in v2.0.1.** Formula cells are detected (Windows: `cell.HasFormula`; Mac: formula string starts with `=`) and reported as a count in the warning panel — "X formula cell(s) skipped". Text-value cells are converted as before; formula cells are never touched.

## U-006 — PowerPoint notes pane and SmartArt
**Speaker notes built in v2.0.1.** Windows (COM): scans and converts `slide.NotesPage.Shapes[2].TextFrame`. Mac: scans `slide.notes.body` (PowerPointApi 1.5+) with graceful fallback on older requirement sets. SmartArt XML remains not exposed via Office.js and is still not converted.

## U-007 — ARM64 Windows build
**Not built.** Only x64 Windows.
**Why:** Needs validation on ARM64 Windows. The x64 build runs under emulation on ARM64 Windows devices.
**Path forward:** Add win-arm64 CI target once validated.

## U-008 — Grammar and spell checking
**Not built.** Mukti only changes encoding, not content. Entirely out of scope.

## U-009 — Community-pluggable mapping profiles
**Not built.** Mapping data is static, maintainer-reviewed JSON.
**Why:** Runtime-loaded mapping plugins are a supply-chain risk and regex-DoS surface. Changes go through PRs with corpus-gate validation (D-0003).

## U-010 — Online / cloud conversion
**Not built.** All conversion is fully offline and on-device.
**Why:** Sending document content to a server would violate the core privacy guarantee.

## U-011 — Selection-only conversion (highlighted text)
**Built in v2.0.1.** "Scan Selection" button added to both Windows (WPF) and Mac (Blazor) UIs. Windows uses `_app.Selection.Range`; Mac uses `ctx.document.getSelection()` for Word and `ctx.workbook.getSelectedRange()` for Excel. PPT selection falls back to full-slide scan.

## U-012 — macOS code signing
**Not built.** The Mac .pkg is unsigned.
**Why:** Apple Developer Program costs USD 99/year. Project has no budget. Users right-click > Open to bypass Gatekeeper.
**Path forward:** Apply to open-source code signing sponsor.

## U-013 — Separate .NET runtime download
**Status: Resolved.** The installer uses dotnet publish --self-contained so the runtime is bundled. No separate .NET download required.
