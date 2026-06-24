# Mukti — Runbook (for the maintainer, GRU-953)

Plain-English operating guide. It grows one section per phase, so the knowledge
is captured as we go (not left to the end). Non-technical: every maintainer task
is a numbered, click-by-click guide. **Started in Phase 3.**

## What you (the maintainer) will ever need to do

A short, bounded list. Nothing else needs a human.

1. **Run the Word test kits** (Spikes D, A, C) — one ~15-minute session, reads
   only. Guides: [`docs/phase0/spikes/`](docs/phase0/spikes/). You paste output
   back to me. *(Pending — do before the build.)*
2. **One-time GitHub setup** — enable Pages, set branch protection, turn on
   secret-scanning + Dependabot. *(Guide added in Phase 4.)*
3. **Pick + point a custom domain** — buy a domain, add a CNAME to GitHub Pages.
   *(Screenshot DNS guide added when we reach hosting; D-0006.)*
4. **Approve the v1.0 release** — review, then I merge + publish. *(Phase 6.)*
5. **Merge Dependabot updates** — green tick = safe to merge; red = leave it for
   me. *(Plain-language guide in `docs/phase2/BUILD-CI.md`.)*

You never hold or paste secret keys into the repo, and you never have to write or
read code.

## Credentials & secrets

- You hold any real credentials (domain registrar login, etc.) on your own
  machine. The automated pipeline holds **no** secrets and never publishes to a
  store on its own.
- The old project's signing key/password are **compromised** — never reuse them.

## If something goes wrong

- **A Word test kit shows RED** → copy the whole console output to me; I adjust
  the plan before any building (that's what the tests are for).
- **The add-in won't load in Word** → it's online-first; check your internet,
  then tell me what Word shows.
- **"Revert" did nothing** → if you edited or saved the document after converting,
  the undo data may be gone; this is expected and disclosed. Keep a copy before
  converting important documents.

## Phase log (what state the project is in)

- Phase 0 ✅ de-risk · Phase 1 ✅ reuse · Phase 2 ✅ design · Phase 3 ✅ review.
- **Now:** awaiting the Word test kits (D/A/C) before Phase 4 build.

*(Detailed RUNBOOK/SETUP/MIGRATION/CONTINUITY guides are completed in Phase 6;
this file is the living seed.)*
