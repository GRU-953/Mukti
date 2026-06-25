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
**Not built.** Conversion covers body text and tables only.
**Why:** WordApi 1.3 does not expose headers/footers for write access. Same as original Mukti known-limitation 1.
**Path forward:** Revisit when WordApi raises requirement set.

## U-005 — Excel formula conversion
**Not built.** Only cell display values converted; formula strings untouched.
**Why:** Converting Bengali text inside formulas risks breaking spreadsheet logic silently.
**Path forward:** Cells from formulas flagged as "formula — skipped" in the preview.

## U-006 — PowerPoint notes pane and SmartArt
**Partially built.** Visible text shapes on slides are converted (Mac + Windows). Speaker notes and SmartArt are not.
**Why:** Speaker notes require a higher WordApi requirement set on Mac. SmartArt XML is internal and not exposed via Office.js.

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
**Not built in v2.** Converts the whole document.
**Why:** Office.js selection events add complexity. Whole-document scan is the safe MVP.
**Path forward:** v2.1 — add "Convert selection" button.

## U-012 — macOS code signing
**Not built.** The Mac .pkg is unsigned.
**Why:** Apple Developer Program costs USD 99/year. Project has no budget. Users right-click > Open to bypass Gatekeeper.
**Path forward:** Apply to open-source code signing sponsor.

## U-013 — Separate .NET runtime download
**Status: Resolved.** The installer uses dotnet publish --self-contained so the runtime is bundled. No separate .NET download required.
