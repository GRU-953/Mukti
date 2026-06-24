# Phase 2 — Build & CI design

**Status:** Design only. No production build code is written in this phase; the
config snippets below are *illustrative* — they show the shape and intent of the
real files, which land in the implementation phase. This document is the
contract those files must satisfy.

**Scope.** How Mukti is laid out, built reproducibly, linted, tested, gated,
released, and kept secure — without repeating the Phase 0 infrastructure
mistakes. Manifest contents (URLs, IDs, `<Requirements>`, icons) are owned by a
separate **`MANIFEST-DESIGN.md`**; this file only references the manifest where
build/release touches it.

**Grounds.** Builds directly on
[`phase0/DO-NOT-REPEAT.md`](../phase0/DO-NOT-REPEAT.md) (esp. C1 secrets, M5
repo hygiene/`npm ci`/no-CI/no-Dependabot/no-CVE-gate, M5 `curl|bash`),
[`DECISION-LOG.md`](../DECISION-LOG.md) (D-0002 WordApi 1.3, D-0004 burned
credentials, D-0005 Noto Sans Bengali, D-0006 hosting domain, D-0007 idempotency
guard), [`phase1/REUSE-VALIDATION.md`](../phase1/REUSE-VALIDATION.md), and the
existing pure-Node corpus harness under
[`tools/corpus/`](../../tools/corpus/). The corpus gate **runs the harness and
freeze tools that already exist** — it does not reinvent them.

---

## 0. Principles (the non-negotiables this design enforces)

1. **The conversion engine is pure TypeScript with ZERO Office.js.** It runs
   under plain Node, is unit-tested and corpus-tested with no Word present, and
   a **lint rule fails the build** if anything in the engine imports Office.js.
   (Phase 0 G4: the prior engine core was already Node-runnable; we make that a
   hard, enforced boundary.)
2. **Office.js is never bundled.** It is loaded from Microsoft's official CDN at
   runtime via a `<script>` tag in the taskpane HTML, and is marked *external*
   to the bundler so it can never be pulled into our artifact.
3. **Reproducible = pinned + locked + checksummed, not bit-for-bit.** Pinned
   Node (`.nvmrc` + `engines`), exact lockfile, `npm ci`, and a published
   **sha256** of the release artifact. We do *not* promise byte-identical
   rebuilds (see §2.4).
4. **Nothing secret, built, or cached lives in git.** Already enforced by
   [`.gitignore`](../../.gitignore). Dev TLS certs are generated at build time.
5. **The pipeline holds no secrets and never self-publishes to a store.** It
   builds, gates, and (on a tag) deploys static files to GitHub Pages. Any real
   credential lives with the maintainer (D-0004). CI starts working only after a
   one-time maintainer setup (§8).

---

## 1. Repository / build layout

One repo, clear module boundaries. The boundary that matters most — engine vs
everything Office — is **lint-enforced** (§5.3), not just convention.

```
Mukti/
├─ .nvmrc                      # pins Node major+minor (toolchain)
├─ package.json                # "engines", scripts, deps; "type":"module"
├─ package-lock.json           # exact dependency tree (committed)
├─ .gitignore                  # secrets/artifacts/caches (already present)
├─ eslint.config.js            # flat config; hosts the no-Office.js boundary rule
├─ tsconfig.json               # base; project refs below
├─ .github/
│  ├─ workflows/ci.yml         # build + lint + test + corpus gate + CVE + secret scan
│  ├─ workflows/release.yml    # tag → package → checksum → Pages deploy
│  └─ dependabot.yml           # npm + github-actions update PRs
├─ src/
│  ├─ engine/                  # ── PURE TYPESCRIPT, ZERO Office.js ──
│  │  ├─ index.ts              #   public API: convert(input): string (+ report)
│  │  ├─ pipeline.ts           #   map → reorder → NFC (Spike-B rules, D-0007)
│  │  ├─ reorder.ts            #   pre-base vowel / reph reordering
│  │  └─ guards.ts             #   "no Bijoy source glyph → no-op", URL/email skip
│  ├─ host/                    # ── Office.js GLUE (the only place it's allowed) ──
│  │  ├─ word.ts               #   per-run read/write, context.sync() batching
│  │  └─ scan.ts               #   known-font detection, "not scanned" reporting
│  └─ taskpane/                # ── UI (HTML/CSS/TS); imports host + engine ──
│     ├─ taskpane.html         #   <script> CDN tag for Office.js lives HERE
│     ├─ taskpane.ts           #   wires buttons → host → engine; preview/revert
│     └─ styles.css
├─ data/                       # ── STATIC DATA (no code) ──
│  ├─ bijoy-to-unicode.json    #   the adapted map (REUSE-VALIDATION: ADAPT)
│  ├─ known-fonts.json         #   the curated ~138-name Bijoy list (no fuzzy match)
│  └─ schema/*.schema.json     #   JSON Schemas validated in `test`
├─ manifest/
│  ├─ manifest.xml             #   Word-only, WordApi 1.3 (owned by MANIFEST-DESIGN.md)
│  └─ assets/                  #   ribbon icons
├─ corpus/                     # ── FROZEN GOLD STANDARD (already exists) ──
│  ├─ visible/*.jsonl          #   what the engine may be built against
│  ├─ heldout/heldout.jsonl    #   never read while building (gate-only)
│  └─ MANIFEST.json            #   sha256 + counts; `freeze --check` verifies
├─ tools/
│  └─ corpus/                  # ── PURE-NODE HARNESS (already exists) ──
│     ├─ harness.mjs           #   runCorpus + fuzz + CLI (the gate runner)
│     ├─ freeze.mjs            #   MANIFEST writer / --check
│     ├─ metrics.mjs           #   char/word accuracy, NFC, exact
│     └─ reference-converter.mjs  # Spike-B oracle (100% baseline)
└─ docs/…                      # planning + design (this file under phase2/)
```

### 1.1 Module dependency direction (allowed import arrows)

```
        data/  ───────────────┐
                              ▼
   taskpane/  ──▶  host/  ──▶ engine  ──▶ (Node/std only)
       │                        ▲
       └────────────────────────┘   taskpane may import engine directly (preview)

   Office.js (window.Word/Office)  ──▶ allowed ONLY in host/ and taskpane/
```

Rules the lint config enforces (§5.3):

- **`src/engine/**` may import:** other `src/engine/**`, `data/**` (JSON only),
  and Node/standard-library modules. **Never** `office-js`, the `Word`/`Office`
  globals, anything under `src/host/**` or `src/taskpane/**`, or DOM types.
- **`src/host/**`** may import `engine` and `office-js`; it is the only TS that
  talks to Word.
- **`src/taskpane/**`** may import `host` and `engine`; it owns the CDN script
  tag and the DOM.

This is what makes "the engine runs under Node CI with no Word" *true by
construction*: the harness (`tools/corpus/harness.mjs`) `import()`s a converter
module that exports `convert`, and the engine satisfies that signature with no
Office.js anywhere in its transitive imports.

---

## 2. Toolchain pinning + reproducibility recipe

### 2.1 Pin the toolchain

- **`.nvmrc`** pins the Node line (e.g. an active LTS, `22`). `nvm use` reads it.
- **`package.json` `engines`** restates it so `npm` warns/errors on the wrong
  Node, and CI asserts it.

```jsonc
// package.json (illustrative excerpt)
{
  "name": "mukti",
  "type": "module",
  "engines": { "node": ">=22.0.0 <23", "npm": ">=10" },
  "packageManager": "npm@10.x"
}
```

```yaml
# CI uses the exact .nvmrc line — no drift between local and CI
- uses: actions/setup-node@v4
  with: { node-version-file: ".nvmrc", cache: "npm" }
```

### 2.2 Lock dependencies, install with `npm ci`

- **`package-lock.json` is committed** and is the source of truth.
- **Every install is `npm ci`** (clean, lockfile-exact, fails if lock and
  `package.json` disagree). This directly fixes Phase 0 M5
  (`npm install --legacy-peer-deps`, no `npm ci`, no `engines`/`.nvmrc`).
- `--legacy-peer-deps` is **banned**; peer conflicts are fixed, not bypassed.

### 2.3 The reproducibility recipe (commands)

```bash
nvm use                 # adopt the pinned Node from .nvmrc
node -v                 # sanity-check it matches engines
npm ci                  # install EXACTLY the locked tree (no resolution)
npm run lint            # boundary + style; fails on Office.js in engine
npm test                # unit tests + JSON-schema validation of data/
npm run corpus:gate     # harness over visible AND heldout; fuzz; freeze --check
npm run build           # type-check + bundle taskpane/host; Office.js external
npm run package         # assemble the publishable static site into dist/
npm run checksum        # write dist/SHA256SUMS over the artifact
```

Anyone with the repo at a given commit + the pinned Node + the committed
lockfile reproduces the same *inputs*; `checksum` fingerprints the *output*.

### 2.4 Why checksum, not bit-for-bit

Bit-for-bit reproducible builds are a high-cost guarantee (deterministic
timestamps, sorted archive entries, pinned every transitive tool, stripped build
metadata). For a static client-side add-in it buys little: there is no installer
to tamper with at rest beyond the hosted files, and users verify integrity
against a **published sha256** of the release artifact. So the promise is:
**same pinned toolchain + locked deps + `npm ci`** (inputs reproducible) **and a
published `SHA256SUMS`** (output verifiable) — *not* byte-identical rebuilds.
This is honest and matches the project's "no overclaiming" stance (contrast
Phase 0 H2's fake accuracy claim).

---

## 3. The npm scripts (exact)

```jsonc
// package.json "scripts" (illustrative; flags are load-bearing)
{
  "scripts": {
    "build":        "tsc -b && vite build",            // type-check, then bundle
    "lint":         "eslint . && tsc -b --noEmit",      // boundary rule + types
    "test":         "vitest run && node tools/validate-data.mjs",
    "corpus:visible":"node tools/corpus/harness.mjs --dir corpus/visible --converter dist-engine/index.mjs --threshold 0.99",
    "corpus:heldout":"node tools/corpus/harness.mjs --dir corpus/heldout --converter dist-engine/index.mjs --threshold 0.99",
    "freeze:check": "node tools/corpus/freeze.mjs --check",
    "corpus:gate":  "npm run freeze:check && npm run corpus:visible && npm run corpus:heldout",
    "package":      "node tools/package.mjs",           // assemble dist/ static site
    "checksum":     "node tools/checksum.mjs",          // dist/SHA256SUMS (sha256)
    "certs":        "node tools/dev-certs.mjs",         // generate dev TLS at build time
    "start":        "npm run certs && vite"             // local dev server (HTTPS)
  }
}
```

Notes that bind these to existing assets:

- `corpus:visible` / `corpus:heldout` invoke the **existing**
  [`tools/corpus/harness.mjs`](../../tools/corpus/harness.mjs) CLI with its real
  flags (`--dir`, `--converter`, `--threshold`). The harness already:
  - returns/exits non-zero unless **char ≥ 0.99 AND word ≥ 0.99 AND zero NFC
    failures AND zero idempotency failures AND zero no-op failures**, and
  - runs the seeded **fuzz** (2000 inputs; must never throw, output must be NFC).
  CI does not re-implement any of this; it just runs the script and checks the
  exit code.
- `--converter` points at the **compiled engine** (a Node-loadable
  `index.mjs`), proving the same engine that ships also passes the gate. A tiny
  pre-step (`tsc` of just `src/engine`) produces `dist-engine/index.mjs`; this
  is the only thing the gate needs and it carries **zero Office.js**.
- `freeze:check` runs the **existing**
  [`tools/corpus/freeze.mjs --check`](../../tools/corpus/freeze.mjs): it fails if
  `corpus/MANIFEST.json` is stale, i.e. someone changed corpus data without a
  deliberate re-freeze. This protects the gold standard's integrity.
- `certs` generates **dev** TLS at build time and writes to a gitignored path
  (`certs/`), never committed (Phase 0 C1; D-0004).

---

## 4. CI workflow design

Two workflows. **`ci.yml`** gates code (runs on push + PR). **`release.yml`**
publishes (runs only on a version tag). Merge protection requires `ci.yml` to be
green; release is a separate, tag-triggered event.

### 4.1 Triggers

| Workflow | Trigger | Purpose |
|---|---|---|
| `ci.yml` | `push` to `main`, `pull_request` to `main` | Gate every change |
| `release.yml` | `push` tag `v*.*.*` | Package + checksum + deploy to Pages |

### 4.2 What blocks **merge** vs blocks **release**

| Gate | Job | Blocks merge? | Blocks release? |
|---|---|---|---|
| Install integrity (`npm ci`, lockfile vs package.json) | `build` | ✅ | ✅ |
| TypeScript type-check | `build` | ✅ | ✅ |
| **No-Office.js-in-engine** lint rule (§5.3) | `lint` | ✅ | ✅ |
| Style/lint (rest of ESLint) | `lint` | ✅ | ✅ |
| Unit tests + data schema validation | `test` | ✅ | ✅ |
| **Corpus gate** (visible + heldout, ≥99% char/word, NFC/idem/no-op/fuzz) | `corpus` | ✅ | ✅ |
| **`freeze --check`** (corpus manifest in sync) | `corpus` | ✅ | ✅ |
| Bundle-size budget (taskpane JS/CSS ceiling) | `build` | ✅ | ✅ |
| **CVE gate** — high/critical in *shipped* deps with reachable path (§5.1) | `security` | ✅ | ✅ |
| Secret scan (no key/cert/token committed) | `security` | ✅ | ✅ |
| CVE in *dev-only* deps | `security` | ⚠️ warn | ⚠️ warn |
| Cold-start ≤ ~2s, per-`context.sync()` budget (§7) | — | measured-later | measured-later |

"Measured-later" items are explicitly **not asserted now** — they are set by the
calibration spike (Phase 0 M3) and become CI checks only once targets exist. The
one performance thing CI *can* enforce today is a **bundle-size budget**, a
reasonable proxy for cold-start.

### 4.3 Ordering

```
        ┌────────┐
        │  build │  (npm ci → tsc → vite build → bundle-size budget)
        └───┬────┘
            │ (artifacts: dist/, dist-engine/)
   ┌────────┼─────────┬──────────────┐
   ▼        ▼         ▼              ▼
 ┌────┐  ┌──────┐  ┌────────┐   ┌──────────┐
 │lint│  │ test │  │ corpus │   │ security │   ← run in parallel after build
 └────┘  └──────┘  └────────┘   └──────────┘
            (all four must pass; PR cannot merge otherwise)
```

`lint`, `test`, `corpus`, and `security` run in parallel for fast feedback;
`build` runs first because the corpus gate needs the compiled engine and the
size budget needs the bundle. Branch protection on `main` requires all five jobs
green.

### 4.4 `ci.yml` — illustrative skeleton

```yaml
name: ci
on:
  push: { branches: [main] }
  pull_request: { branches: [main] }
permissions:
  contents: read            # least privilege; CI never needs write here
concurrency:                # cancel superseded runs on the same ref
  group: ci-${{ github.ref }}
  cancel-in-progress: true

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-node@v4
        with: { node-version-file: ".nvmrc", cache: "npm" }
      - run: npm ci
      - run: npm run build
      - run: node tools/size-budget.mjs   # fail if taskpane bundle > budget
      - uses: actions/upload-artifact@v4
        with: { name: build, path: "dist\ndist-engine", retention-days: 7 }

  lint:
    needs: build
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-node@v4
        with: { node-version-file: ".nvmrc", cache: "npm" }
      - run: npm ci
      - run: npm run lint                  # includes the no-Office.js-in-engine rule

  test:
    needs: build
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-node@v4
        with: { node-version-file: ".nvmrc", cache: "npm" }
      - run: npm ci
      - run: npm test

  corpus:
    needs: build
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-node@v4
        with: { node-version-file: ".nvmrc", cache: "npm" }
      - run: npm ci
      - uses: actions/download-artifact@v4
        with: { name: build }
      - run: npm run corpus:gate           # freeze --check + visible + heldout + fuzz

  security:
    needs: build
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-node@v4
        with: { node-version-file: ".nvmrc", cache: "npm" }
      - run: npm ci
      - run: node tools/cve-gate.mjs       # osv-scanner + allowlist policy (§5.1)
      - uses: gitleaks/gitleaks-action@v2   # secret scan (also enable native push protection)
```

### 4.5 `release.yml` — illustrative skeleton

Tag-triggered. Re-runs the full gate (a tag must not ship something `main`
wouldn't), then packages, checksums, and deploys the **static site** to GitHub
Pages. **No store publishing; no secrets consumed** — Pages deploy uses GitHub's
built-in OIDC token, not a stored credential.

```yaml
name: release
on:
  push: { tags: ["v*.*.*"] }
permissions:
  contents: write    # attach SHA256SUMS to the GitHub Release
  pages: write       # deploy site
  id-token: write    # OIDC for Pages — no long-lived secret
jobs:
  release:
    runs-on: ubuntu-latest
    environment: github-pages
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-node@v4
        with: { node-version-file: ".nvmrc", cache: "npm" }
      - run: npm ci
      - run: npm run lint && npm test && npm run corpus:gate   # gate again on the tag
      - run: npm run build && npm run package
      - run: npm run checksum                                   # dist/SHA256SUMS
      - uses: softprops/action-gh-release@v2                    # publish SHA256SUMS + notes
        with: { files: "dist/SHA256SUMS" }
      - uses: actions/upload-pages-artifact@v3
        with: { path: "dist" }
      - uses: actions/deploy-pages@v4
```

---

## 5. Security gates

### 5.1 CVE policy

**Tooling.** Primary gate: **`osv-scanner`** (reads `package-lock.json`, queries
the OSV database, understands dev-vs-prod scope and has a first-class
expiring-allowlist mechanism). `npm audit` is acceptable as a lightweight
fallback but its severity mapping is noisier; OSV is the gate of record. The
"reachable path" judgement is applied by **scoping to shipped deps** (the
dependency tree that ends up in `dist/`) — a vuln only in `devDependencies`
cannot reach a user.

**Policy table** (severity × where the dep ships × action):

| Severity | Shipped dep (in `dist/`) | Dev-only dep |
|---|---|---|
| Critical | **Block** build/release | Warn (annotate PR) |
| High | **Block** build/release | Warn |
| Moderate | Warn + open tracking issue | Warn |
| Low / informational | Warn | Ignore |

**"Reachable path."** A shipped dep is one present in the production bundle. If a
flagged shipped dep is genuinely not reachable (e.g. a code path we never call),
that is recorded as a **time-boxed allowlist entry**, not a silent pass.

**Expiring allowlist for false positives.** A single committed file with an
explicit expiry and rationale per entry; the gate **fails on an expired or
malformed entry** so suppressions cannot rot.

```jsonc
// security/cve-allowlist.json (illustrative)
{
  "entries": [
    {
      "id": "GHSA-xxxx-yyyy-zzzz",
      "package": "some-transitive-dep",
      "reason": "Vuln is in a CLI path we never invoke; advisory has no fix yet.",
      "approvedBy": "maintainer",
      "expires": "2026-09-01"      // gate FAILS once this date passes → forces re-review
    }
  ]
}
```

This directly closes Phase 0 M5 ("no CVE gate") and gives non-experts a paper
trail instead of permanent, forgotten suppressions.

### 5.2 Secret scanning

- **Native GitHub secret scanning + push protection** enabled on the repo (part
  of §8 maintainer setup) — blocks known credential patterns at push time.
- **`gitleaks`** in CI as defence-in-depth, scanning the diff for keys/certs/
  tokens. Fails the `security` job on a hit.
- `.gitignore` already blocks `*.key *.pem *.p12 *.pfx *.cer *.crt certs/
  signing/ .env*` (Phase 0 C1 / D-0004). Dev TLS certs are generated by
  `npm run certs` into the gitignored `certs/`.

### 5.3 The no-Office.js-in-engine lint rule

**Rule of record: ESLint `no-restricted-imports`** scoped to `src/engine/**`,
backed by **`eslint-plugin-import`'s `import/no-restricted-paths`** to also block
cross-layer *relative* imports (engine → host/taskpane). `no-restricted-imports`
catches the package/global; `no-restricted-paths` catches sneaking Office in via
a host file.

```js
// eslint.config.js (flat config, illustrative)
import importPlugin from "eslint-plugin-import";

export default [
  // 1) Engine may NOT import Office.js (package OR the office-addin shim).
  {
    files: ["src/engine/**/*.ts"],
    rules: {
      "no-restricted-imports": ["error", {
        paths: [
          { name: "office-js", message: "Engine must be pure: no Office.js." },
          { name: "@microsoft/office-js", message: "Engine must be pure: no Office.js." }
        ],
        // also forbid the live-binding globals if ever re-exported as a module
        patterns: ["**/host/**", "**/taskpane/**"]
      }]
    }
  },
  // 2) Enforce layer direction with path zones (engine is a leaf).
  {
    plugins: { import: importPlugin },
    rules: {
      "import/no-restricted-paths": ["error", {
        zones: [
          { target: "src/engine", from: "src/host",     message: "engine→host forbidden" },
          { target: "src/engine", from: "src/taskpane", message: "engine→taskpane forbidden" }
        ]
      }]
    }
  }
];
```

**What it catches (example).** This fails `npm run lint`, which fails the `lint`
job, which blocks merge:

```ts
// src/engine/pipeline.ts  — ILLEGAL
import { Word } from "office-js";          // ✗ no-restricted-imports → error
import { readRuns } from "../host/word";   // ✗ no-restricted-paths   → error
```

Because the global `Word`/`Office` objects are *ambient* (not imports), the
boundary is reinforced two more ways: (a) the engine's `tsconfig` does **not**
include `office-js` types, so any reference to `Word`/`Office` is a *type error*
that fails `tsc`; (b) the corpus gate `import()`s the compiled engine under bare
Node where those globals do not exist — if the engine touched them it would
throw, failing the gate. Lint is the primary, fast signal; the type-check and
the Node-only gate are belt-and-braces.

---

## 6. Dependabot

### 6.1 Config

```yaml
# .github/dependabot.yml (illustrative)
version: 2
updates:
  - package-ecosystem: "npm"
    directory: "/"
    schedule: { interval: "weekly" }
    open-pull-requests-limit: 5
    groups:                         # fewer, batched PRs = less load on a solo maintainer
      dev-dependencies:
        dependency-type: "development"
      prod-minor-patch:
        dependency-type: "production"
        update-types: ["minor", "patch"]
    labels: ["dependencies"]
  - package-ecosystem: "github-actions"   # keep CI actions current/pinned
    directory: "/"
    schedule: { interval: "weekly" }
```

Closes Phase 0 M5 ("no Dependabot"). Grouping keeps the PR volume sane for a
single non-technical maintainer; the **full CI gate runs on every Dependabot
PR**, so a bad update can't merge.

### 6.2 Plain-language mini-guide — "How to merge a Dependabot PR safely"

> **You don't need to read the code.** Dependabot opens a PR to update one of
> Mukti's building blocks (often a security fix). Your job is to let the robots
> check it, then merge if they're happy.
>
> 1. **Open the PR.** Read the title — it says what's updated and from/to which
>    version. The description links the changelog; you can skim it, but you
>    don't have to.
> 2. **Wait for the checks.** At the bottom you'll see CI running. Look for a
>    green tick ✓ next to **build, lint, test, corpus, security**.
>    - **All green ✓ → safe to merge.** Click **Merge**. The tests, the 99%
>      accuracy gate, and the security scan all passed on the new version.
>    - **Any red ✗ → do NOT merge.** Something broke. Leave the PR open and
>      comment `@dependabot ignore this minor version` only if it's noise;
>      otherwise wait — Dependabot often supersedes it with a better update, or
>      ask for help. A red check is the system protecting you.
> 3. **If it's a security update (labelled as such) and it's green, merge it
>    promptly** — that's the whole point of the gate.
> 4. **Never** click "merge without waiting for checks" / "bypass." The checks
>    are the safety net.
>
> Rule of thumb: **green = go, red = leave it.** You can always ignore a PR; you
> can never un-ship a broken merge as easily.

---

## 7. Performance budgets

Per Phase 0 M3, performance is **budgeted per `context.sync()`** (round-trip),
not per word, and the actual targets come from a **calibration spike** — they
are *not* asserted now.

| Budget | Target | Where checked |
|---|---|---|
| Taskpane cold start | ~≤ 2s | **measured-later** (calibration spike) |
| Conversion cost | budgeted **per `context.sync()`**, batch reads/writes (emulate the Excel pattern, Phase 0 G5/H5) | **measured-later** |
| Taskpane bundle size | a fixed ceiling (proxy for cold start) | **CI-enforced now** via `tools/size-budget.mjs` in the `build` job |

So: bundle-size budget is a real, blocking CI gate today; cold-start and
per-sync latency are documented targets that become CI checks once the spike
sets numbers. We do not fabricate numbers (consistent with the no-overclaiming
stance behind Phase 0 H2).

---

## 8. Release / versioning + checksum + manifest re-sideload rule

### 8.1 Versioning

- **SemVer** on git tags: `vMAJOR.MINOR.PATCH`. `package.json` `version` matches
  the tag. A tag is the only thing that triggers `release.yml`.
- Release notes are generated on the GitHub Release; they call out **any
  manifest change** explicitly (see §8.3).

### 8.2 Artifact checksum

- `npm run checksum` writes **`dist/SHA256SUMS`** (sha256 over the packaged
  artifact files). It is attached to the GitHub Release.
- Users / the install guide can verify a download with
  `sha256sum -c SHA256SUMS`. This is the integrity promise from §2.4 and closes
  Phase 0 M5 ("no checksums").

### 8.3 The "manifest change requires re-sideload" rule

The manifest is installed into each user's Word by sideloading. **Changing
certain manifest fields forces every user to re-sideload** (re-install) — there
is no silent push. Therefore:

- **Hosting URLs use a stable custom domain** (D-0006; details in
  `MANIFEST-DESIGN.md`), so we can move hosting (e.g. between Pages and another
  static host) by repointing DNS **without** changing the manifest — no
  re-sideload. GitHub Pages is the host; a `CNAME` file in the Pages artifact
  binds the custom domain, and the manifest URLs point at the domain, not at
  `*.github.io`.
- **Release rule:** any change to manifest **Id, requirement set, permissions,
  or the source-location host/domain** is a **breaking, re-sideload release**.
  It must be:
  1. called out in the release notes with a plain-language "you must
     re-install" banner, and
  2. (ideally) carried as a MINOR/MAJOR bump, never a quiet PATCH.
- Pure code/asset changes behind the *same* domain ship transparently (users get
  them on next launch) and do **not** require re-sideload.

`MANIFEST-DESIGN.md` owns the field list, the exact custom-domain/CNAME wiring,
and the WordApi 1.3 `<Requirements>` block (D-0002). This document only owns the
*release-process consequence*.

---

## 9. One-time maintainer credential setup (high level)

The pipeline holds **no secrets** and **never self-publishes to a store**
(D-0004). CI/CD only starts working after the maintainer does a small,
one-time, repository-settings setup — none of which puts a long-lived secret
into the pipeline:

1. **Enable GitHub Pages** for the repo and select **"GitHub Actions"** as the
   Pages source. (Lets `release.yml` deploy via OIDC — no token to store.)
2. **Add the custom domain** in Pages settings and create the matching **DNS
   records** at the domain registrar (D-0006). This is the only place the
   maintainer touches DNS.
3. **Turn on branch protection** for `main`: require the `ci.yml` checks
   (`build`, `lint`, `test`, `corpus`, `security`) to pass before merge; forbid
   bypass.
4. **Enable native secret scanning + push protection** (reinforces §5.2).
5. **Confirm Dependabot is on** (the committed `.github/dependabot.yml` enables
   the PRs; the maintainer just confirms alerts/security updates are enabled in
   settings).

What is **deliberately NOT** set up here: any code-signing key, store-publisher
credential, or API token in CI. If code signing is ever added, the **private
key stays on the maintainer's machine**, signing happens locally, and only the
signature/checksum is published — never the key (Phase 0 C1; D-0004). The
pipeline's maximum authority is "deploy static files to our own Pages site,"
granted by short-lived OIDC, not a held secret.

---

## 10. Traceability — which Phase 0/1 finding each gate closes

| Finding | Closed by |
|---|---|
| C1 committed signing key + plaintext password | §0.4, §5.2, §8/§9 (no secret in repo or pipeline; dev certs built locally) |
| H2 fake "100% accuracy" claim | §3/§4 corpus gate runs the real harness on visible **and** heldout at ≥99% with NFC/idempotency/fuzz |
| H3 non-idempotent engine | corpus gate's idempotency + no-op + NFC checks (already in `harness.mjs`); D-0007 |
| H4/H5 silent font mangling/skips | engine/host split (§1), known-font data in `data/`, "not scanned" reporting (host layer) |
| M3 ~3 syncs/paragraph | §7 per-`context.sync()` budget + Excel-style batching |
| M5 no `npm ci`/`engines`/`.nvmrc`/CI/Dependabot/CVE/checksums; committed binaries; `curl|bash` | §2 (pin+lock+`npm ci`), §4 (CI), §6 (Dependabot), §5.1 (CVE gate), §8.2 (checksum), `.gitignore` (no binaries); install is sideload via manifest, never `curl|bash` |
| G4 engine already Node-runnable | made a **lint-enforced** boundary (§5.3) |
```
