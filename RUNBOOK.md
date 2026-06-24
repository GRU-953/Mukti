# Mukti — Runbook (for the maintainer, GRU-953)

Plain-English operating guide. It grows one section per phase, so the knowledge
is captured as we go (not left to the end). Non-technical: every maintainer task
is a numbered, click-by-click guide. **Started in Phase 3.**

> **How to read this file.** You never have to write or read code, and you never
> paste a secret key into the repository. Each task below is a numbered list of
> exactly what to open, click and type, followed by a **"How to know it worked"**
> check. Where a button might be named slightly differently in your version of
> the website, that is flagged — labels drift, so look for the nearest match.
>
> **A word on jargon.** Any technical word is explained in one line the first
> time it appears, and every term is also in
> [`docs/GLOSSARY.md`](docs/GLOSSARY.md). If you hit a word that isn't explained,
> that's a bug in the docs — tell me.

## What you (the maintainer) will ever need to do

A short, bounded list. Nothing else needs a human.

1. **Run the Word test kits** (Spikes D, A, C) — one ~15-minute session, reads
   only. Guides: [`docs/phase0/spikes/`](docs/phase0/spikes/). You paste output
   back to me. *(Do this before the build; see [§1](#1-run-the-word-test-kits-spikes-d-a-c).)*
2. **One-time GitHub setup** — enable Pages, set branch protection, turn on
   secret-scanning + push protection, confirm Dependabot.
   *(Full guide: [§2](#2-one-time-github-setup).)*
3. **Pick + point a custom domain** — buy a domain, add a DNS record to GitHub
   Pages, enter it in settings, point the build at it.
   *(Full guide: [§3](#3-custom-domain-d-0006).)*
4. **Make a release** — create a version tag (or use the Releases page); the
   robots build it and publish it. *(Full guide: [§4](#4-making-a-release).)*
5. **Merge Dependabot updates** — green tick = safe to merge; red = leave it for
   me. *(Full guide: [§5](#5-merging-a-dependabot-update-safely).)*

You never hold or paste secret keys into the repo, and you never have to write or
read code.

---

## 1. Run the Word test kits (Spikes D, A, C)

These are short, read-only experiments that prove the foundation works in real
Word *before* we build on it. (A "spike" is just a small, time-boxed test of one
risky question.) Each kit has its own step-by-step guide and a snippet that
prints a clear **GREEN** or **RED** line, so the result never depends on anyone's
memory.

1. Open the spikes folder: [`docs/phase0/spikes/`](docs/phase0/spikes/).
2. Run them in this order — **D first** (it is the existential one), then **A**,
   then **C**:
   - [`spike-D-encoding.md`](docs/phase0/spikes/spike-D-encoding.md)
   - [`spike-A-per-run-font.md`](docs/phase0/spikes/spike-A-per-run-font.md)
   - [`spike-C-undo-revert.md`](docs/phase0/spikes/spike-C-undo-revert.md)
3. Run **each kit twice**: once in **Word on your desktop** and once in **Word on
   the web** (the version in your browser). Decision D-0016 requires all three to
   be GREEN on **both** before the v1.0 release.
4. Copy the **whole console output** from each run and paste it back to me.

**How to know it worked:** each snippet prints a line beginning `RESULT:`. If
every kit prints GREEN (or "LIKELY OK") on both desktop and web, the foundation
is sound. If any prints **RED**, do not worry and do not change anything — paste
it to me; a RED result is the test doing its job and it reshapes the plan before
any code is written.

---

## 2. One-time GitHub setup

You do this **once**, after the code is in the repository, to switch on hosting
and the safety nets. None of it puts a secret into the project — it only flips
settings on the GitHub website. Budget about 15 minutes.

Throughout: a **repository** ("repo") is the project's home on GitHub; **CI**
("continuous integration") is the set of automatic checks the robots run on every
change; the **`main` branch** is the official, live copy of the code.

Start here every time:

1. In your browser, go to **https://github.com/gru-953/mukti**.
2. Sign in if asked.
3. Click the **Settings** tab. It is on the top row of tabs, on the right
   (the small gear/cog). *(If you don't see it, you may be signed in as the wrong
   account — only the owner sees Settings.)*

### 2a. Turn on GitHub Pages (the free hosting)

"GitHub Pages" is GitHub's free website hosting; it is where Mukti's files live.
We use the **"GitHub Actions"** source, which lets the release robot publish the
site itself with no password to store.

1. In **Settings**, in the left-hand menu, click **Pages**.
2. Under **Build and deployment**, find the **Source** dropdown.
3. Click it and choose **GitHub Actions**. *(Not "Deploy from a branch" — that's
   the older way. The label may read "GitHub Actions" or "GitHub Actions
   (beta)".)*
4. There is nothing to save — it applies immediately.

**How to know it worked:** the **Source** box now reads **GitHub Actions**. (No
website will appear yet — that happens the first time you make a release in
[§4](#4-making-a-release).)

### 2b. Protect the `main` branch

This stops anything from going live unless the robots' checks have passed first.

1. In **Settings**, in the left-hand menu, click **Branches**. *(It may sit under
   a "Code and automation" heading. On some accounts this is now called
   **Rules → Rulesets**; if so, click **Branches** under Rulesets and follow the
   same idea — "require checks before merge".)*
2. Click **Add branch protection rule** (or **Add rule** / **Add ruleset**).
3. In the **Branch name pattern** box, type exactly: `main`
4. Tick **Require a pull request before merging**. (A "pull request" is the
   review screen where a change waits before joining `main`.)
5. Tick **Require status checks to pass before merging**.
6. In the search box that appears below it, type `verify` and click the **verify**
   check to add it to the required list. *(This is the name of Mukti's CI job —
   the one that runs lint, tests and the accuracy gate. If you don't see it yet,
   it appears after CI has run at least once; come back and add it then.)*
7. Tick **Do not allow bypassing the above settings** (or "Include
   administrators"), so the rule applies to everyone — including you.
8. Click **Create** (or **Save changes**) at the bottom.

**How to know it worked:** the Branches page now lists a rule for `main`, and on
any future pull request you'll see "Required" next to the **verify** check, with a
**Merge** button that stays disabled until it passes.

### 2c. Turn on Secret scanning + Push protection

"Secret scanning" watches for passwords or keys accidentally committed;
"push protection" blocks them *before* they ever land. This reinforces the rule
that no secret ever lives in this repo.

1. In **Settings**, in the left-hand menu, click **Code security**. *(It may read
   **Code security and analysis** or **Advanced Security**.)*
2. Find **Secret scanning** and click **Enable**.
3. Find **Push protection** (usually just below it) and click **Enable**.

**How to know it worked:** both rows now show **Enabled** (a green tick or an
"Enabled"/"Disable" toggle). From now on, if anyone ever tries to push a key,
GitHub stops them with a clear message.

### 2d. Confirm Dependabot is on

"Dependabot" is GitHub's robot that opens small update requests when one of
Mukti's building blocks needs a refresh (often a security fix). The project
already ships its configuration in
[`.github/dependabot.yml`](.github/dependabot.yml); you just confirm the alerts
are switched on.

1. Still on the **Code security** page (from §2c).
2. Find **Dependabot alerts** and make sure it is **Enabled**.
3. Find **Dependabot security updates** and make sure it is **Enabled** too.

**How to know it worked:** both rows read **Enabled**. Within a week or so you may
see the first automatic update appear under the repo's **Pull requests** tab,
labelled `dependencies` — that's normal; [§5](#5-merging-a-dependabot-update-safely)
tells you what to do with it.

---

## 3. Custom domain (D-0006)

**Why this matters (read this first).** Mukti is installed into each person's
Word by a small file called the **manifest** (it tells Word where the add-in
lives). If that file points at GitHub's raw address (something like
`gru-953.github.io`), then the day we ever move hosting, *every user would have to
re-install*. So D-0006 makes it a hard rule: v1.0 must ship on a **custom domain**
you own — a stable web address like `add-in.yourdomain.org` — that we can quietly
re-point to a different host later **without anyone re-installing**. The raw
GitHub address is for development only.

A "**domain**" is a web address you rent (e.g. `yourdomain.org`). A "**DNS
record**" is one line in the domain's settings that says "this address points
there". A "**subdomain**" is a prefix on your domain (e.g. the `add-in.` part of
`add-in.yourdomain.org`). A "**CNAME record**" is the specific type of DNS line
that points one address at another.

### 3a. Buy a domain

1. Go to any domain registrar (a company that rents domains — for example
   Namecheap, Cloudflare, Gandi, Porkbun; any will do).
2. Search for a name you like and can keep long-term (the whole point is that it's
   stable). Cheap and boring is fine.
3. Buy it and sign in to the registrar's control panel.

**How to know it worked:** the domain shows as **Active** / **Registered** in your
registrar account.

### 3b. Add the DNS record pointing your subdomain at GitHub Pages

You will point a subdomain — we'll use **`add-in`** as the example — at GitHub's
Pages host.

1. In your registrar's control panel, open the **DNS** / **DNS records** /
   **Manage DNS** section for your domain. *(Wording varies by registrar.)*
2. Click **Add record** (or **Add new record**).
3. Fill the record in like this:
   - **Type:** `CNAME`
   - **Name** / **Host:** `add-in` *(just the subdomain prefix — the registrar
     adds your full domain automatically. Some registrars want the full
     `add-in.yourdomain.org`; follow their on-screen hint.)*
   - **Value** / **Target** / **Points to:** `gru-953.github.io` *(GitHub's Pages
     host — note: no `https://`, no trailing slash, and a full stop at the end is
     fine if the registrar adds one.)*
   - **TTL:** leave the default (e.g. "Automatic" or 3600).
4. Click **Save** / **Add**.

**How to know it worked (DNS has "propagated"):** DNS changes take a few minutes
to a few hours to spread across the internet ("propagation"). To check, open a new
browser tab and go to:
**`https://dnschecker.org/#CNAME/add-in.yourdomain.org`** (put your real
subdomain in). When most of the world map shows green ticks pointing at
`gru-953.github.io`, it has propagated. *(No special tools needed — it's a free
website.)*

### 3c. Enter the domain in GitHub Pages settings

1. Go to **https://github.com/gru-953/mukti** → **Settings** → **Pages** (as in
   [§2a](#2a-turn-on-github-pages-the-free-hosting)).
2. Find the **Custom domain** box.
3. Type your full subdomain, e.g. `add-in.yourdomain.org`, and click **Save**.
4. GitHub now runs a "DNS check". When it passes, tick **Enforce HTTPS** if it is
   offered (this makes the site load securely; it may take a little while to
   become available).

**How to know it worked:** under the Custom domain box you see **"DNS check
successful"** (a green tick), and shortly after, **Enforce HTTPS** is tickable and
ticked. *(If it says the check failed, wait for propagation from §3b and click
**Save** / re-check again — it is almost always just DNS not having spread yet.)*

### 3d. Point the build at the https URL

The build needs to know the final public address so the manifest and the code
load from your domain, not the raw GitHub address. This is one setting called
**`MUKTI_BASE_URL`** ("the base web address Mukti is served from").

1. Go to **Settings** → **Secrets and variables** → **Actions**.
2. Click the **Variables** tab (next to "Secrets"). *(This is a plain
   **variable**, not a secret — it holds a public web address, nothing sensitive.)*
3. Click **New repository variable**.
4. **Name:** type exactly `MUKTI_BASE_URL`
5. **Value:** type your full https address, e.g. `https://add-in.yourdomain.org`
   *(with `https://`, no trailing slash).*
6. Click **Add variable**.

**How to know it worked:** the **Variables** tab now lists `MUKTI_BASE_URL` with
your address. The **next** release (see [§4](#4-making-a-release)) will build the
manifest and site against this domain. Tell me once it's set so I can confirm the
manifest in the release points at your domain and not at `*.github.io`.

---

## 4. Making a release

A "**release**" is a published, version-numbered copy of Mukti that users install
from. You don't build anything by hand — you give the project a **version tag**
(a label like `v1.0.0`), and the release robot does the rest: it re-runs all the
checks, builds the site, publishes it to your domain, and attaches the manifest
and a **checksum** (a fingerprint users can use to confirm their download wasn't
tampered with).

> **Versions look like `v1.0.0`** — three numbers (this is "SemVer", short for
> "semantic versioning"). Bigger first number = bigger change. For v1.0, use
> exactly `v1.0.0`.

### 4a. Create the release (the easy way — the Releases page)

1. Go to **https://github.com/gru-953/mukti**.
2. On the right-hand side of the main page, click **Releases**. *(If you don't see
   it, add `/releases` to the address bar.)*
3. Click **Draft a new release**.
4. Click the **Choose a tag** dropdown, type the version exactly — `v1.0.0` — and
   click **Create new tag: v1.0.0 on publish**.
5. Leave the **Target** as `main`.
6. In **Release title**, type something plain, e.g. `Mukti v1.0.0`.
7. In the description box, write a short, human note of what changed. **If this
   release changes the manifest in any way, you must include a clear banner** (see
   the box below).
8. Click **Publish release**.

That tag is what starts the robot. (If you ever prefer the developer way, creating
the tag from a terminal — `git tag v1.0.0 && git push origin v1.0.0` — does
exactly the same thing. The Releases page is the recommended route for you.)

**How to know it worked:**
1. Go to the **Actions** tab of the repo (top row). You'll see a run named
   **release** start, with a spinning amber dot.
2. Wait for it to finish with a **green tick** (usually a few minutes). A red
   cross means the release was blocked by a failing check — nothing went live;
   tell me and I'll look.
3. Back on the **Releases** page, your `v1.0.0` release now lists attached files,
   including `manifest.xml` and `SHA256SUMS` (the checksum file).
4. Open your domain `https://add-in.yourdomain.org` in a browser — it should load
   the published site, not a "404 not found" page.

> ### ⚠️ The re-install (re-sideload) rule — important
>
> Mukti is installed by "sideloading" the manifest (installing it by hand rather
> than from a store). **Most updates are silent** — users get new code and fixes
> automatically next time they open Word, with nothing to do.
>
> **BUT** if a release changes the manifest itself — its identity, the
> permissions it asks for, the Word features it needs, or the web address it
> loads from — then **every user must re-install (re-sideload) the new
> `manifest.xml`**. There is no way to push that silently.
>
> So, when a release changes the manifest:
> 1. Put a plain banner at the top of the release notes, e.g.:
>    > **Please re-install:** this version changes the add-in's setup. Download
>    > the new `manifest.xml` from this release and follow
>    > [INSTALL.md](INSTALL.md) again. Your documents are unaffected.
> 2. Treat it as a meaningful version bump (the middle or first number goes up,
>    e.g. `v1.1.0` — never a quiet last-number-only change).
>
> Choosing a stable custom domain ([§3](#3-custom-domain-d-0006)) is exactly what
> lets us *avoid* this for hosting moves. If you're unsure whether a change
> touches the manifest, ask me before publishing.

---

## 5. Merging a Dependabot update safely

**You don't need to read the code.** Dependabot opens a pull request to update one
of Mukti's building blocks (often a security fix). Your job is to let the robots
check it, then merge only if they're happy. (A "pull request", or PR, is the
review screen where a change waits before joining the live code.)

1. Go to **https://github.com/gru-953/mukti** and click the **Pull requests** tab
   (top row).
2. Click a PR whose author is **dependabot** (it'll be labelled `dependencies`).
   The title says what's being updated and from/to which version. You can skim the
   linked changelog, but you don't have to.
3. Scroll to the bottom of the PR, to the **checks** area. You'll see Mukti's CI
   running.
   - **Green tick ✓ next to `verify` (and "All checks have passed") → safe to
     merge.** This means the tests, the ≥99% accuracy gate, and the security scan
     all passed on the new version. Click the green **Merge pull request** button,
     then **Confirm merge**.
   - **Red cross ✗ → do NOT merge.** Something broke on the new version. **Leave
     the PR open** and tell me. A red check is the system protecting you; don't
     try to fix it yourself.
   - **Amber dot (still spinning) → wait.** The checks haven't finished. Come back
     in a few minutes.
4. **Never** click any "merge without waiting for checks", "bypass" or
   "administrator override" option. The checks are the safety net.

**Rule of thumb: green = go, red = leave it.** You can always ignore a PR safely;
you can't easily un-ship a broken merge.

**How to know it worked:** after merging, the PR shows a purple **Merged** label,
and Dependabot may automatically close any related older update. You don't need to
do anything else.

---

## Credentials & secrets

This is the single most important safety rule, carried from the forensic findings
(D-0004): the prior project committed a live signing key and its password into the
code for everyone to see.

- **You hold any real credentials on your own machine** — your domain registrar
  login, your GitHub login. They live with you, never in the project.
- **The automated pipeline holds no secrets.** The release robot publishes to your
  own GitHub Pages using a short-lived, automatic GitHub token (it expires almost
  immediately and can only deploy the site) — there is no password stored anywhere
  for you to leak. It never publishes to an app store on its own.
- **Never reuse the old (compromised) signing key, certificate or password.** They
  are treated as permanently burned. If code signing is ever added later, the key
  stays on your machine, signing happens locally, and only the signature/checksum
  is published — never the key.
- **You never paste a secret into the repository, a release note, or an issue.**
  If you ever feel asked to, stop and check with me — that's a red flag.

---

## If something goes wrong

- **A Word test kit shows RED** → copy the whole console output to me; I adjust
  the plan before any building (that's what the tests are for).
- **The add-in won't load in Word** → it's online-first; check your internet,
  then tell me what Word shows.
- **"Revert" did nothing** → if you edited or saved the document after converting,
  the undo data may be gone; this is expected and disclosed. Keep a copy before
  converting important documents.
- **A release run shows a red cross in the Actions tab** → nothing went live; the
  checks blocked a bad release on purpose. Copy the name of the failing step to me.
- **The Pages "DNS check" keeps failing** → it's almost always just DNS not having
  spread yet; wait a bit ([§3b](#3b-add-the-dns-record-pointing-your-subdomain-at-github-pages))
  and re-check. Double-check the CNAME **Value** is `gru-953.github.io` with no
  `https://`.
- **Dependabot PR is red** → leave it open, tell me. Never bypass the checks.

---

## Phase log (what state the project is in)

- Phase 0 ✅ de-risk · Phase 1 ✅ reuse · Phase 2 ✅ design · Phase 3 ✅ review.
- **Now:** awaiting the Word test kits (D/A/C) before Phase 4 build.

*(Detailed RUNBOOK/SETUP/MIGRATION/CONTINUITY guides are completed in Phase 6;
this file is the living seed.)*

---

## Related guides

- **Release gate before v1.0:** [`docs/RELEASE-CHECKLIST.md`](docs/RELEASE-CHECKLIST.md)
  — the boxes you tick before publishing.
- **Installing Mukti (for end users):** [`INSTALL.md`](INSTALL.md).
- **Plain-language glossary:** [`docs/GLOSSARY.md`](docs/GLOSSARY.md).
- **CI/build/security design:** [`docs/phase2/BUILD-CI.md`](docs/phase2/BUILD-CI.md).
