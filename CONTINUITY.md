# Mukti — Continuity

**Who this is for.** The maintainer (a non-technical person) and anyone trying to
keep Mukti alive in the years ahead. It is written in plain language. It explains
where everything lives, how the project keeps running mostly on its own, what the
single biggest danger is, and what to do if you ever want to hand the project to
someone else.

If you are a developer (or an AI) taking over the *code*, read
[`MIGRATION.md`](MIGRATION.md) instead — that is the technical onboarding pack.
For your day-to-day click-by-click tasks, see [`RUNBOOK.md`](RUNBOOK.md).

---

## 1. What is where (a map of the project)

Mukti is one repository, split into a few clearly separated parts. You do not
need to understand the code inside them — just what each part is *for*.

| Part | Folder | In plain words |
|---|---|---|
| **The engine** | `src/engine/` | The actual conversion brain: it turns old Bijoy/SutonnyMJ text into modern Unicode Bengali. It is pure logic — it knows nothing about Word. This is the heart of the project. |
| **The host** | `src/host/` | The glue that talks to Microsoft Word: it reads the text out of your document, asks the engine to convert it, shows a preview, applies the change, and can undo it. |
| **The UI** | `src/taskpane/` and `src/commands/` | What you actually see and click — the bilingual panel inside Word (Bangla by default, English toggle) and the ribbon button. |
| **The test set** | `corpus/` | A frozen collection of known-correct examples. It is the official scorecard: every change is measured against it, and nothing ships unless it passes. |
| **The conversion table** | `data/` | The list of which old glyph becomes which Unicode letter. Plain data, separate from the code, so it can be checked and extended on its own. |
| **The design + decisions** | `docs/` | Why every choice was made. The most important files are the decision log (`docs/DECISION-LOG.md`), the architecture (`docs/phase2/`), the review (`docs/phase3/REVIEW.md`), the plain-English glossary (`docs/GLOSSARY.md`), and the known-limitations list (`docs/KNOWN-LIMITATIONS.md`). |
| **The automation** | `.github/`, `tools/`, `scripts/` | The robots that check, build, and publish the project (see §2). |

The golden rule of the layout: **the engine never touches Word.** That separation
is not just tidiness — it is what lets the engine be tested automatically, in
full, without anyone opening Word. It is enforced by an automatic check, so it
cannot quietly erode.

---

## 2. How it stays alive

Mukti is designed to need a human only rarely. Most of the upkeep is automatic.

**Hosting.** The add-in is a set of static files published to **GitHub Pages**.
Word loads them when it starts (Mukti is online-first; there is no offline mode).
Crucially, users reach Mukti through a **custom domain** (a web address the
project owns), not the raw `github.io` address. This means hosting can be moved
later — to a different provider — just by repointing the domain, **without forcing
every user to re-install** (D-0006). Picking and registering that domain name is
the one decision still outstanding before launch.

**Automated checks (CI).** Every time the code changes, GitHub runs a set of
checks automatically (see `docs/phase2/BUILD-CI.md` and `.github/workflows/ci.yml`).
They confirm the build works, the engine stays pure, the tests pass, the **test
set still scores ≥99%**, and **no security problem or secret** has crept in. If
anything fails, the change is blocked. You read a green tick or a red cross — you
never read the code.

**Dependabot.** Mukti is built on top of other people's software building blocks.
Dependabot watches those for updates and security fixes and opens a tidy pull
request when one is available. The full set of automated checks runs on each of
those too, so a bad update cannot slip through. The plain-language "how to merge a
Dependabot pull request safely" guide is in `docs/phase2/BUILD-CI.md` (§6.2). The
rule of thumb: **green = merge, red = leave it.**

**What needs a human, and how rarely.** Almost nothing day to day. The complete
list of things only a human can do is in `RUNBOOK.md`; in short:

- Merge the occasional Dependabot update (green = safe). Roughly weekly to
  monthly, a few minutes each.
- Approve a release when there is something new to ship. Rare.
- One-time setup tasks (turn on GitHub Pages, branch protection, secret scanning;
  register the domain). Done once.

If nobody touches Mukti for months, it keeps working. The risk is not that it
breaks on its own — it is that an *unmerged security update* sits waiting. That is
why the Dependabot habit matters.

---

## 3. The single biggest risk: the bus factor

Be honest about this, because the whole continuity plan is built around it.

**The maintainer is non-technical.** Today, changes to the TypeScript code are
made with AI build assistance. If that assistance becomes unavailable and no
technical person has stepped in, **no human currently on the project can change
the code.** That is the bus factor: the project's ability to evolve sits in a
fragile place.

This does **not** mean Mukti stops working — the published add-in keeps running,
and the automated checks keep protecting it. It means the project cannot *grow or
be repaired* until a capable developer (or AI) is brought in.

**The mitigation is deliberate and already in place.** It is not "hope someone
shows up"; it is "make sure whoever shows up can succeed in an afternoon":

1. **[`MIGRATION.md`](MIGRATION.md) — the onboarding pack.** A successor can read
   it and understand the whole system quickly, without archaeology.
2. **Frozen contracts** (`src/engine/contracts.ts`, `src/host/contracts.ts`,
   `data/schema/`). These are the fixed, documented boundaries between the parts.
   A successor builds *against* them and cannot accidentally break the shape of
   the system.
3. **The test set as an executable specification** (`corpus/`). It does not just
   *describe* correct behaviour — it *checks* it, automatically. A successor can
   change anything and instantly see, with one command, whether it is still
   correct. The corpus is the project's institutional memory made runnable.

Together these turn "only the original builders understand this" into "anyone
competent can pick it up." Keeping them current is the most valuable continuity
work there is.

---

## 4. Where the credentials live (and the no-secrets rule)

The rule is absolute and it is the lesson of the old, abandoned codebase, which
had a live signing key and its password committed for anyone to find (D-0004):

- **No secret is ever committed to this repository.** Not a key, not a
  certificate, not a password, not a token. Automatic secret scanning blocks them
  at the door.
- **Any real credential lives with the maintainer**, on the maintainer's own
  machine — for example the domain registrar login. They are never pasted into
  the repository or into the automation.
- **The automation holds no secrets.** It can do exactly one privileged thing:
  publish the project's own static files to its own GitHub Pages site, using a
  short-lived permission GitHub grants at the moment of publishing. It cannot
  publish to any app store and it cannot reach anything else.
- The old signing key and password from the previous versions are treated as
  **permanently compromised** and must never be reused.

If you are ever asked to put a secret "just for now" into the repo or the
pipeline, the answer is no. There is always another way.

---

## 5. If you want to hand this project to someone

A short checklist for passing Mukti on cleanly:

1. **Give them this file and [`MIGRATION.md`](MIGRATION.md).** Those two, plus
   `docs/`, are the whole story.
2. **Transfer ownership, not secrets.** Make them an owner/admin of the GitHub
   repository so they control the automated checks and releases. Hand over the
   domain registrar account separately and securely. Never email a key or
   password; rotate anything that was shared during the handover.
3. **Confirm the safety net is intact** before you step away: branch protection
   on, secret scanning on, Dependabot on, the automated checks green. The
   `RUNBOOK.md` setup list covers this.
4. **Point them at the test set.** Tell them: change whatever you like, then run
   the gate — if it stays green at ≥99%, you have not broken the conversion. That
   single fact is what makes it safe for a stranger to take over.
5. **Keep the paper trail going.** The value of `docs/DECISION-LOG.md` is that it
   never stops. Ask the new owner to log their significant decisions the same way.

The project was built so that handover is a normal, low-drama event — not a
crisis. Keep it that way.
