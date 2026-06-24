# Phase 2 — Manifest design

The add-in manifest is the file Word reads to know what Mukti is, where its code
lives, and which buttons to show. This design fixes the manifest decisions; the
actual `manifest.xml` is produced in Phase 4 from a template + build variables.

## Decisions encoded here

| Aspect | Prior (do-not-repeat) | Mukti MVP |
|---|---|---|
| Hosts | Document + Workbook + Presentation | **Word only** (`<Host Name="Document"/>`) |
| Requirement set | **none declared** | **`WordApi` MinVersion `1.3`** (D-0002) |
| URLs | `https://localhost:3000` everywhere | **custom domain**, build-injected |
| Add-in Id | placeholder `a1b2c3d4-…` | fresh GUID `41a04cce-6e7b-45e6-926c-91da857aa888` |
| Provider/support | `anindash` | maintainer (GRU953) + this repo |
| Output font copy | "Kohinoor Bangla" (proprietary) | Noto Sans Bengali (D-0005) |

## Requirement set — why 1.3, declared explicitly

Phase 0 forensics + Phase 1 confirm nothing Mukti does exceeds **WordApi 1.3**
(table access via `cell.body.paragraphs` is the highest; `getTextRanges` for
per-run fonts is 1.3). Declaring the **lowest set that works** maximizes the
range of Word versions Mukti runs on, and makes Word refuse to load Mukti on an
unsupported version rather than failing mysteriously at runtime:

```xml
<Requirements>
  <Sets DefaultMinVersion="1.3">
    <Set Name="WordApi" MinVersion="1.3"/>
  </Sets>
</Requirements>
```

The host adapter also calls `Office.context.requirements.isSetSupported(
"WordApi","1.3")` at startup and shows a clear bilingual message if unsupported
(belt-and-braces; see UI-UX.md).

## Hosting & custom domain (resolves do-not-repeat M2)

The manifest must point at a **stable custom domain** (e.g. `https://<domain>/`)
so the hosting location can move without forcing every user to re-sideload
(spec §11). Strategy:

- Build injects the base URL from a single variable (`MUKTI_BASE_URL`).
  - **Production:** the custom domain (a CNAME to GitHub Pages). Decision D-0006
    (which domain) is still open; the design does not depend on the exact name.
  - **Development:** `https://localhost:3000` (dev server, certs generated at
    build time — never committed).
- `<AppDomains>` lists the production domain; `SourceLocation`, `FunctionFile`,
  icons, and resource URLs all derive from `MUKTI_BASE_URL`.
- A production **manifest template** (`manifest.production.xml`) keeps the domain
  as a token so the published artifact is reproducible.

> **Re-sideload rule (spec §11):** because the Id and the domain are fixed, code
> updates deploy without user action. But **any change to the manifest itself**
> (new Id, new domain, new commands) requires users to re-sideload — so manifest
> changes trigger a re-sideload notice in the release notes and install guide.

## Surface (ribbon + taskpane)

- One `<Host xsi:type="Document">` with a `DesktopFormFactor`.
- A single ribbon group "Mukti" on the Home tab with:
  - **Mukti** button → opens the taskpane (`ShowTaskpane`), `SupportsPinning`.
  - (MVP keeps the primary flow in the taskpane: Scan → Preview → Apply → Revert.
    A direct ribbon "Convert" command is deferred to avoid a no-preview path,
    per do-not-repeat H6.)
- `<Permissions>ReadWriteDocument</Permissions>` (required to edit text/format).
- No shared runtime (so no auto-scan-on-open; that's web-user-initiated /
  desktop-only and out of MVP per spec §7).

## Identity & metadata

- `ProviderName` = maintainer; `SupportUrl` / `GetStarted.LearnMoreUrl` =
  `https://github.com/gru-953/mukti`.
- `DisplayName` "Mukti"; description states the function and the **online-first**
  nature; no "offline" claim.
- Version `0.x` during pre-release; semver thereafter.

## Validation

`office-addin-manifest validate` runs in CI against the built manifest. The
template + variable substitution is covered by a build test so a missing/ð
a malformed `MUKTI_BASE_URL` fails the build rather than shipping localhost (the
exact prior mistake).
