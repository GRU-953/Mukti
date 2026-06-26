# UNBUILT.md — Deliberate omissions in Mukti v2

Read before adding any of these. Each was explicitly considered and rejected.

---

## U-001 — AppSource / Microsoft Store distribution
**Not built.** Mukti is not published to Microsoft AppSource.
**Why:** AppSource requires paid Microsoft Partner Center membership. Unsustainable for a volunteer project.
**Path forward:** GitHub Releases direct download.

## U-002 — Automatic in-app updates
**Update banner built in v2.0.2.** Both Windows (WPF) and Mac (Blazor) check `https://api.github.com/repos/GRU-953/Mukti/releases/latest` at startup and display a yellow banner when a newer release is found. Clicking opens the releases page. The check is fire-and-forget; any network failure is silently swallowed.
**Not built:** Auto-silent-update (download + install without user action). Requires signing certificate continuity and administrator privileges — out of scope for a volunteer project.

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
**Installer built in v2.0.4.** `setup-arm64.iss` produces `Mukti-Setup-2.0.4-arm64.exe`; `register-addin-arm64.ps1` handles HKCU add-in registration.
**What's done:** ARM64 cross-compilation (framework-dependent), `setup-arm64.iss`, `register-addin-arm64.ps1`, ARM64 installer in CI/release.yml, ARM64 installer uploaded to v2.0.4 release.
**What remains:** ARM64-native Office is not yet available from Microsoft (Office runs x64 emulated on Windows ARM64). Full integration testing requires an ARM64 Windows device with future ARM64-native Office. The installer shows a notice explaining this limitation.

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

## U-012 — Code signing (Windows + macOS)
**Abandoned by design.** Both installers ship unsigned and will stay that way. See decision D-0005.
**Why:** Authenticode and Apple Developer signing add cost and process overhead with no benefit for a free, offline, open-source tool whose source and build are fully public. The SignPath Foundation route was dropped entirely.
**User impact:** Windows — SmartScreen "More info → Run anyway"; macOS — right-click → Open to bypass Gatekeeper. Both documented in the README.

## U-013 — Separate .NET runtime download
**Status: Resolved.** The installer uses dotnet publish --self-contained so the runtime is bundled. No separate .NET download required.
