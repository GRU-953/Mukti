# Mukti — Forensic Audit 04: Build/Infra, Manifest, Hosting, Licensing, Secrets, Repo Hygiene

Scope: build/infra, manifest, hosting, licensing, secrets, repo hygiene.
Method: READ-ONLY review of prior versions under
`scratchpad/prior/{Mukti-2.0.0, 2.5.0, 2.5.1, 3.1.0, main}`.
Primary subject = `Mukti-main` (v3.1.0). Paths below are abbreviated to `prior/Mukti-main/...`.
Current repo under audit (`/home/user/Mukti`) contains only `LICENSE` + `README.md` (7 bytes);
remote `origin = .../git/GRU-953/Mukti` — new owner is **GRU-953**, not anindash.

Verdict: **NOT shippable as-is.** Real private keys are committed in all five versions; the
pruduct's output font is proprietary ("Kohinoor Bangla"); manifest targets 3 hosts on
`https://localhost:3000` with no Requirements set; committed build binaries + `.pyc`; no CI/CVE gate.
Privacy claim ("content never leaves device, no telemetry") **holds** — no external egress found.

---

## 1. MANIFEST AUDIT (`prior/Mukti-main/manifest.xml`, `manifest-production-template.xml`)

**Hosts targeted — FAILS "Word ONLY" spec.** All five versions declare three hosts:
- `manifest.xml:25-27` → `<Host Name="Document" />`, `<Host Name="Workbook" />`, `<Host Name="Presentation" />`
- VersionOverrides define full ribbon groups for all three: `MuktiWordGroup` (`:51`), `MuktiExcelGroup` (`:107`), `MuktiPptxGroup` (`:163`). Excel + PowerPoint must be removed.

**Requirement sets — FAILS.** No `<Requirements>` / `<Sets>` / WordApi declared in ANY version
(grep across all 5 manifests + production template returned NONE). Spec wants a declared WordApi set
(e.g. `WordApi 1.3`). Absent entirely.

**Hardcoded URLs — FAILS "custom domain".** `manifest.xml` hardcodes `https://localhost:3000`
everywhere:
- `:16-17` IconUrl / HighResolutionIconUrl
- `:21` `<AppDomain>https://localhost:3000</AppDomain>`
- `:31` SourceLocation `https://localhost:3000/taskpane.html`
- `:211-213` icon images; `:216-217` Taskpane.Url / Commands.Url
`manifest-production-template.xml` uses `https://YOUR_DOMAIN_HERE` placeholder (`:28,33,43,223-229`) —
a manual sed-replace template, not a real custom domain. No domain is actually owned/configured.

**Identity / ProviderName / SupportUrl / Id:**
- `ProviderName` = `Aninda S Howlader` (`manifest.xml:11`) — stale; new owner is GRU-953.
- `SupportUrl` + `GetStarted.LearnMoreUrl` = `https://github.com/anindash/mukti` (`:18`, `:218`) —
  points to OLD owner `anindash`. README instead points to `anindash15-arch/Mukti` (`README.md:12-20,79,89`).
  Three different identity strings in play: `anindash`, `anindash15-arch`, now `GRU-953`. Inconsistent.
- `Id` = `a1b2c3d4-e5f6-7890-abcd-ef1234567890` (`:9`) — an obviously **fake/placeholder GUID**, identical
  in dev and production-template manifests. Must be a real unique GUID; reusing a literal sample GUID
  risks collisions/sideload conflicts.
- `Version` mismatch: manifest.xml says `3.1.0.0` (`:10`) but production-template still says `2.0.0.0` (`:22`).
- `Description` (`:14`) markets the proprietary font: "...to Unicode Bengali (**Kohinoor Bangla**)..."
- `<Permissions>ReadWriteDocument</Permissions>` (`:34`) — appropriate (no over-broad scope).

## 2. HOSTING MODEL

Three documented models (`DISTRIBUTE.md`):
- **localhost (default/dev):** `server/server.js` is a standalone Node HTTPS static server on
  `localhost:3000` serving `dist/`. Notable behaviour: auto-generates a self-signed cert via `openssl`
  if none present (`server.js:101-108`); polls `pgrep`/`tasklist` every 15s and **auto-kills itself**
  when Word/Excel/PowerPoint close (`server.js:172-218`); `/health` endpoint (`:132`). Has directory-
  traversal guard (`:145`). CORS `Access-Control-Allow-Origin: *` (`:121`, also webpack `:73`).
- **GitHub Pages:** `scripts/deploy-github-pages.sh` runs `npm run build`, seds the production template
  with the Pages URL, then **force-pushes `dist/` to `gh-pages`** (`:82 git push --force`). Hardcoded
  example URL `https://anindash.github.io/mukti` (`:9,30`) and commit message pinned to "v2.0.0" (`:81`).
- **Self-host / SharePoint / AppSource:** documented in `DISTRIBUTE.md` §3-7 (manual).

There IS a deploy script (`deploy:github-pages` in `package.json:25`) — it deploys to the repo's own
`gh-pages` branch. This is a **self-publish path**, contrary to the "never self-publish" spec; it should
be removed or gated.

## 3. SECRETS — CRITICAL (committed in ALL FIVE versions, since 2.0.0)

| Path | What it is | Verified | Severity |
|---|---|---|---|
| `prior/*/signing/mukti-codesign.key` | **Unencrypted** RSA-4096 PRIVATE KEY (`-----BEGIN PRIVATE KEY-----`, no passphrase) | `openssl pkey` parses it; pubkey MD5 **matches** `mukti-codesign.crt` exactly → this is the live signing key | CRITICAL |
| `prior/*/signing/mukti-codesign.p12` | PKCS#12 bundle, password documented in repo | Real PKCS#12 (MAC SHA1, 2048 iters); password `«REDACTED-PASSWORD»` confirmed correct | CRITICAL |
| `prior/*/signing/mukti-codesign.crt` | Self-signed code-signing cert; CN=Mukti Developer, O=Aninda S Howlader, C=BD; valid Mar 2026–Mar 2028; SHA1 `80:D5:E4:9A...` | n/a (public) | (public, but ties identity) |
| `prior/*/certs/localhost.key` | **Unencrypted** RSA-2048 PRIVATE KEY for localhost TLS | parses cleanly | HIGH |
| `prior/*/certs/localhost.crt` | Self-signed localhost cert | n/a | low |

Additional secret-disclosure: **`signing/README.md:10,15,25` prints the .p12 password `«REDACTED-PASSWORD»` in
plaintext** three times. (Password is moot anyway — the standalone `.key` is unencrypted.)

Severity rationale: the standalone `mukti-codesign.key` is an unencrypted PEM, so anyone with the repo
can sign macOS/Windows binaries as "Mukti Developer" — supply-chain forgery risk. These are in git
history across all five tags, so deletion alone is insufficient.

**Required actions:** (a) treat all four private keys as compromised → **rotate/revoke** the codesign cert;
(b) `git rm` + **history rewrite** (filter-repo/BFG) — they exist in every prior tag; (c) add `*.key`,
`*.p12`, `*.pem`, `*.pfx`, `certs/`, `signing/` to `.gitignore`; (d) generate dev/localhost certs at
build/install time (the codebase already can: `server.js:87` + `office-addin-dev-certs`); (e) remove the
password from `signing/README.md`. Spec compliance ("you never hold secrets / never self-publish"): both
violated.

## 4. LICENSING

- **LICENSE** (`prior/Mukti-main/LICENSE`): clean **MIT**, "Copyright (c) 2026 Aninda S Howlader."
  `package.json:9` also `"license": "MIT"`. Copyright holder is stale vs new owner GRU-953.
- **Bundled fonts:** NONE. No `.ttf/.otf/.woff/.woff2` committed in any version (find returned nothing).
  So no font-binary licensing violation *today* — but spec wants openly-licensed bundled fonts and none
  are provided.
- **PROPRIETARY FONT "Kohinoor Bangla" — FAILS spec.** It is hardcoded as the conversion **output**
  font (`const TARGET_UNICODE_FONT = "Kohinoor Bangla"`), so every converted document is retagged to a
  proprietary Apple/Linotype font:
  - `src/office/word-processor.js:24`
  - `src/office/excel-processor.js:21`
  - `src/office/pptx-processor.js:21`
  - `src/commands/commands.js:11`
  - CSS UI font stack: `src/taskpane/taskpane.css:29` (`"Kohinoor Bangla", "Noto Sans Bengali", "SolaimanLipi"`)
  - User-facing copy: `manifest.xml:14,229`, `manifest-production-template.xml:26`,
    `taskpane.html:52,204`, `taskpane.js:201,889`, `README.md` desc.
  Fix: change default output font to an OFL font (Noto Sans Bengali / SolaimanLipi) and scrub all copy.
  (Note: `detector.js:121-122` merely *lists* "kohinoor bangla*" as a recognized Unicode input font —
  that's acceptable; the problem is using it as the assigned output.)
- **Third-party data — 454K-word dictionary, NO attribution / provenance.** `dictionary.js:2-3` says
  "454K word list" loaded from `/data/words.txt`, but **`words.txt` is not committed in any version**
  (webpack `:64` copies it with `noErrorOnMissing:true`). No source, license, or attribution comment
  anywhere. A 454K Bengali wordlist is almost certainly derived from an external corpus — provenance and
  license MUST be established before redistribution. Unresolved licensing risk.

## 5. PRIVACY / TELEMETRY — PASSES

"Document content never leaves the device; no telemetry" is **supported by the code**:
- Full `src/` egress sweep for `fetch(`/`http(s)://`: only two external refs, both the legitimate
  **Office.js CDN** `https://appsforoffice.microsoft.com/lib/1/hosted/office.js`
  (`taskpane.html:7`, `commands.html:7`).
- Only other `fetch` is **local**: `dictionary.js:23 fetch("/data/words.txt")` (same-origin static file).
- No analytics/telemetry/beacon SDKs (no gtag/GA/mixpanel/posthog/amplitude/sentry/sendBeacon).
- Learned corrections + stats persist to **`localStorage`** only (`learning-store.js:27-53,152-163`) — on-device.
- `server/server.js` makes no outbound calls; it's a local static server + process monitor.
Conclusion: telemetry claim **confirmed**. (Minor: CORS `*` on the dev server is broad but standard for Office add-ins.)

## 6. REPO HYGIENE

- **Committed build binaries — FAIL.** `dist-pkg/Mukti-2.0.0-Windows.zip` (1.75 MB) and
  `dist-pkg/Mukti-2.0.0-macOS.dmg` (1.79 MB) committed in ALL five versions. `.gitignore` lists
  `dist-pkg/` (`.gitignore:2,5`) yet the artifacts are tracked anyway → were force-added or committed
  before ignore. ~3.5 MB of stale binaries per tag, version-frozen at 2.0.0 while source is at 3.1.0.
- **Committed `__pycache__` — FAIL.** `installer/__pycache__/install.cpython-313.pyc` present in all five
  versions. Not in `.gitignore`. Should be ignored/removed.
- **`.gitignore` inadequate** (`prior/Mukti-main/.gitignore`): only `node_modules/`, `dist-pkg/` (dup),
  `.DS_Store`, `dist/`. Missing: `__pycache__/`, `*.pyc`, and (critically) `*.key`/`*.p12`/`*.pem`/`certs/`/`signing/`.
- **node_modules:** not committed (good).
- **Lockfile:** `package-lock.json` present in all versions (`lockfileVersion: 3`, 11,940 lines) — good,
  but build scripts use `npm install --legacy-peer-deps` (README:91, DISTRIBUTE:43, install.py:133), NOT
  `npm ci` → lockfile not enforced; `--legacy-peer-deps` masks dependency conflicts.
- **Python installer risk** (`installer/install.py`, 546 lines): a tkinter/CLI GUI that runs
  `npm install --legacy-peer-deps` (`:133`), `npx office-addin-dev-certs install` (`:158`),
  `npm run build`, and copies `manifest.xml` into Office `wef/` dirs (macOS `:172`, Windows `:192`).
  Risk: arbitrary npm execution + cert install on the user's machine; bundles a packaged `.pyc`. It is a
  convenience auto-installer, not malicious, but expands the install-time trust/attack surface.
- **Remote-install scripts use insecure TLS / `curl|bash`:** `scripts/install-manifest.sh:34` uses
  `curl -fsSL -k` (`-k` = skip cert verification) and `:35` `wget --no-check-certificate`;
  `DISTRIBUTE.md:29` and `install.sh:4` advertise `curl ... | bash` one-liners. Pipe-to-shell + disabled
  TLS verification is a MITM/supply-chain hazard.

## 7. REPRODUCIBILITY & CI — FAIL

- **No CI at all:** no `.github/` directory in any version; no GitHub Actions workflows.
- **No Dependabot / Renovate / CVE gate / `npm audit` step** anywhere.
- **No toolchain pinning:** no `engines` field in any `package.json`; no `.nvmrc` / `.node-version`.
- Lockfile exists but is **not enforced** (`npm install --legacy-peer-deps`, never `npm ci`).
- **No artifact checksums** (no SHA256SUMS / signing manifest for releases).
- Reproducible-build criteria (pinned Node + frozen lockfile via `npm ci` + checksum): **all unmet.**

## 8. TOP RISKS — DO-NOT-REPEAT LIST (ranked)

1. **[CRITICAL] Committed live private keys.** `signing/mukti-codesign.key` (unencrypted RSA-4096,
   matches the signing cert) + `mukti-codesign.p12` (pw `«REDACTED-PASSWORD»`, documented in `signing/README.md`)
   + `certs/localhost.key` — in git history of all five versions. Rotate/revoke + history-scrub +
   gitignore. Never commit secrets; generate certs at build time.
2. **[CRITICAL/LICENSING] Proprietary "Kohinoor Bangla" as the conversion OUTPUT font** (word/excel/pptx
   processors + commands.js + UI + manifest copy). Switch default to an OFL font (Noto Sans Bengali /
   SolaimanLipi) before any FOSS release.
3. **[HIGH/LICENSING] 454K-word dictionary with no provenance/attribution/license** (`dictionary.js`,
   `words.txt` not even committed). Establish source + license before redistribution.
4. **[HIGH] Manifest non-compliant:** targets Word+Excel+PowerPoint (must be Word-only); no
   `<Requirements>`/WordApi set; everything on `https://localhost:3000`; placeholder Id
   `a1b2c3d4-...`; stale ProviderName/SupportUrl (`anindash`) vs new owner GRU-953.
5. **[HIGH] Self-publish + insecure install paths:** `deploy:github-pages` force-pushes a `gh-pages`
   site (violates "never self-publish"); `curl|bash` installers with `-k`/`--no-check-certificate`.
6. **[MEDIUM] Committed binaries + `__pycache__`:** ~3.5 MB stale `dist-pkg/*.dmg/*.zip` and a `.pyc` in
   every version; `.gitignore` too thin.
7. **[MEDIUM] No reproducible build / no CI / no CVE gate:** no Actions, no Dependabot, no `engines`/
   `.nvmrc`, `npm install --legacy-peer-deps` instead of `npm ci`, no artifact checksums.
8. **[LOW] Identity drift & version mismatch:** three owner strings (anindash / anindash15-arch /
   GRU-953); MIT copyright still names Aninda; manifest.xml 3.1.0.0 vs production-template 2.0.0.0;
   server self-reports "v1.0.0".

GOOD (keep): MIT license is clean; **no telemetry / no external egress** (privacy claim holds);
no font binaries illegally bundled; lockfile present; server has dir-traversal guard; no committed node_modules.
