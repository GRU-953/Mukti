# Decision Log

A running record of every significant decision: what was decided, when, why, and
who owns it. "Owner" is who is accountable for the decision, not who typed it.

Statuses: **Decided** · **Proposed** (awaiting maintainer sign-off) · **Open**
(needs a decision).

---

### D-0001 — Clean-room rebuild, not continuation of the prior codebase
- **Status:** Decided
- **Date:** 2026-06-22
- **Owner:** Maintainer (GRU953), per project spec
- **Decision:** Start a fresh, clean-room rebuild. Adopt prior code only where it
  scores against the frozen corpus (Phase 1). Record verdicts in the reuse
  manifest.
- **Why:** Phase 0 forensics found the prior versions share one buggy engine, a
  critical committed secret, a proprietary output font, and an unverifiable
  accuracy claim. Continuing them would inherit all of that. See
  [`phase0/DO-NOT-REPEAT.md`](phase0/DO-NOT-REPEAT.md).

### D-0002 — Target WordApi requirement set 1.3
- **Status:** Decided
- **Date:** 2026-06-22
- **Owner:** Agent (technical), per spec ("pin to the lowest set actually used")
- **Decision:** The manifest will declare `<Requirements>` with WordApi
  **MinVersion 1.3**.
- **Why:** Forensic check of the prior Office.js usage confirms nothing needs
  more than 1.3 (table access via `cell.body.paragraphs` is the highest); 1.3 is
  the lowest set that covers the MVP, maximising Word-version compatibility.
  ([`phase0/findings/03-officejs.md`](phase0/findings/03-officejs.md).)

### D-0003 — MVP scope frozen to Bijoy→Unicode in Word only
- **Status:** Decided
- **Date:** 2026-06-22
- **Owner:** Maintainer (GRU953), per project spec
- **Decision:** No Excel/PowerPoint, no grammar checker, no proofreader, no
  reverse conversion, no dictionary in the MVP. Those are deferred/cut per the
  spec.
- **Why:** Prior scope creep buried the actual product; the converter never
  improved while peripheral features multiplied.

### D-0004 — Treat the prior code-signing key and password as permanently compromised
- **Status:** Decided
- **Date:** 2026-06-22
- **Owner:** Maintainer (GRU953)
- **Decision:** The signing key, `.p12`, certificates, and the password found in
  the prior versions must never be reused. No secret is ever committed to this
  repository; dev certs are generated at build time; the maintainer holds any
  real credentials and the pipeline holds none.
- **Why:** A live, unencrypted code-signing key (and its plaintext password) were
  committed to every prior version. ([`phase0/DO-NOT-REPEAT.md`](phase0/DO-NOT-REPEAT.md) §C1.)

---

## Open decisions (need maintainer input)

### D-0005 — Canonical Unicode output font
- **Status:** Decided
- **Date:** 2026-06-22
- **Owner:** Maintainer (GRU953)
- **Decision:** Bundle **Noto Sans Bengali** (SIL OFL) as the single canonical
  Unicode output font. Its `OFL.txt`/NOTICE will be retained alongside the
  bundled font file.
- **Why:** Maintainer's choice. Widest device coverage, neutral/clean, actively
  maintained by Google/SIL, and the example named in the project spec. The font
  does not affect conversion accuracy (output Unicode codepoints are identical
  regardless of rendering font); it can be changed later at the cost of
  re-bundling and re-testing.

### D-0006 — Hosting domain
- **Status:** Open (not blocking until Phase 2)
- **Owner:** Maintainer (GRU953)
- **Question:** The manifest needs a stable **custom domain** so hosting can move
  without forcing every user to re-install. Options: register a domain (e.g.
  `mukti.<something>`) or, as an interim, use the GitHub Pages URL and accept a
  future re-sideload. Recommendation: secure a custom domain before first public
  release; use GitHub Pages for development.
- **Blocks:** Production manifest (Phase 2+).
