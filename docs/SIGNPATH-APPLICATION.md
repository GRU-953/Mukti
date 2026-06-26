# SignPath Foundation — Free Code Signing Application

Apply at: https://about.signpath.io/product/open-source

This file contains all the details you need to fill in the application form.
Copy-paste the relevant sections below.

---

## Application details

**Organisation name:** Mukti Open Source

**Project name:** Mukti

**Project URL (source code):** https://github.com/GRU-953/Mukti

**Project URL (website / releases):** https://github.com/GRU-953/Mukti/releases

**Short description (one line):**
Free, offline Microsoft Office add-in that converts Bijoy/SutonnyMJ-encoded Bengali text to Unicode — for Windows and Mac.

**Long description:**
Mukti is an open-source Microsoft Office add-in for Windows and Mac that converts legacy Bijoy/SutonnyMJ-encoded Bengali text to Unicode in-place, directly inside Word, Excel, and PowerPoint — including tables, headers/footers, footnotes, formula-free Excel cells, and speaker notes. All conversion runs locally on the user's device using a C# engine built on a 188-entry glyph map validated by 387 corpus tests and a clean scan of 306,620 real-world Bijoy runs from 757 Office documents. No document content ever leaves the device. The project targets non-technical Bengali-speaking users who receive documents in Bijoy encoding and need them in Unicode without installing font packages or learning command-line tools.

**Licence:** MIT (see LICENSE file in repository)

**Programming language(s):** C# (.NET 8)

**Build system:** dotnet publish + Inno Setup 6

**CI/CD:** GitHub Actions (see .github/workflows/release.yml)

---

## What to sign

**File to sign:** `Mukti-Setup-2.0.6.exe` and future releases (Inno Setup output — PE .exe)

**Signing policy:** `release-signing` (Authenticode, SHA-256)

**Artifact configuration:** `inno-setup` (PE file, standard .exe)

---

## After approval — GitHub Actions secrets to add

Go to: https://github.com/GRU-953/Mukti/settings/secrets/actions

Add these two secrets (values provided by SignPath after approval):

| Secret name | Value |
|-------------|-------|
| `SIGNPATH_API_TOKEN` | (from SignPath dashboard) |
| `SIGNPATH_ORGANIZATION_ID` | (from SignPath dashboard) |

Once added, every push of a `v*.*.*` tag triggers `release.yml` which:
1. Builds the Inno Setup `.exe`
2. Uploads it to SignPath Foundation for signing
3. Downloads the signed `.exe`
4. Creates a GitHub Release with the signed installer attached

> **Note (v2.0.4):** Signing has been temporarily removed from `release.yml` while the SignPath connector-url configuration is pending. The release workflow now ships unsigned installers directly. Re-add signing by restoring the `signpath/github-action-submit-signing-request@v1` step.

---

## Notes

- The signing step in `release.yml` is conditional: `if: ${{ secrets.SIGNPATH_API_TOKEN != '' }}`
  → Releases work without signing (unsigned .exe uploaded); signing adds automatically once secrets are set.
- The SignPath artifact configuration slug in `release.yml` is `inno-setup` — set this up in SignPath dashboard.
- The project slug in `release.yml` is `mukti` — match this when creating the project in SignPath.
