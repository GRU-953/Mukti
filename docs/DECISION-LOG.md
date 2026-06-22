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

### D-0007 — Idempotency rule: only transform text containing Bijoy source glyphs
- **Status:** Decided
- **Date:** 2026-06-22
- **Owner:** Agent (technical), validated by Spike B
- **Decision:** The conversion engine must treat any run that contains **no Bijoy
  source glyphs** as a no-op (return it unchanged). Reordering must never run on
  already-Unicode text.
- **Why:** Spike B proved a blind regex reorder is correct on first pass but
  corrupts on a second pass (e.g. দেশ → দশে) — the exact bug class that sank the
  prior engine. Guarding on the presence of source glyphs makes
  `convert(convert(x)) === convert(x)` and `convert(unicode) === unicode` hold
  across the entire corpus. The harness tests this on every case.
  ([`phase0/spikes/spike-B-reordering.md`](phase0/spikes/spike-B-reordering.md).)

### D-0008 — Whitespace and formatting are preserved, never munged
- **Status:** Decided
- **Date:** 2026-06-22
- **Owner:** Agent (technical), per do-not-repeat M-items
- **Decision:** The engine preserves whitespace verbatim and does not "tidy"
  spacing/newlines. (The reference oracle collapses runs of spaces; we do not.)
- **Why:** Formatting fidelity is a core promise; the prior maps munged
  whitespace. Encoded as POLICY override cases in the corpus
  (`edge-space`, `edge-unicode-noop`).

### D-0009 — Reuse decision: adopt the prior map data, rewrite all processing
- **Status:** Decided
- **Date:** 2026-06-22
- **Owner:** Agent (technical), validated by Phase 1 corpus measurement
- **Decision:** Salvage the prior `BIJOY_TO_UNICODE_MAP` **data** (port to
  schema-validated JSON, fix the broken dari entry, re-verify in Phase 4).
  **Rewrite** the entire pipeline (mapping application, reorder, normalization)
  in the TypeScript engine, carrying the Spike-B design rules. Keep the curated
  ~138-name font list (drop fuzzy matching); validate it at the Office layer.
- **Why:** Run as-is against the frozen corpus, the prior engine scores
  96.96%/96.20% (visible) — below the gate — failing idempotency, NFC, dari,
  URL protection, and whitespace preservation, while the map data itself is
  100% first-pass correct. The clean-room Spike B converter scores 100%.
  ([`phase1/REUSE-VALIDATION.md`](phase1/REUSE-VALIDATION.md).)

### D-0010 — Layered architecture with a lint-enforced pure engine
- **Status:** Decided (Phase 2; pending Phase 3 review)
- **Date:** 2026-06-22
- **Owner:** Agent (technical)
- **Decision:** Three layers — pure TS `engine` (zero Office.js, Node-gated by
  the corpus harness), a thin Office.js `host` adapter, and the taskpane UI.
  The engine→Office.js boundary is enforced by an ESLint rule that fails the
  build. Frozen interface contracts live in `src/engine/contracts.ts` and
  `src/host/contracts.ts`. ([`phase2/ARCHITECTURE.md`](phase2/ARCHITECTURE.md).)
- **Why:** Inverts the prior entanglement of conversion with Office.js; makes the
  engine testable without Word and the platform risk containable.

### D-0011 — Revert is snapshot-based, not programmatic undo
- **Status:** Decided (Phase 2; pending Spike C)
- **Date:** 2026-06-22
- **Owner:** Agent (technical)
- **Decision:** "Revert Mukti changes" restores a pre-apply snapshot (text +
  per-run formatting) stored in the document settings. Ctrl+Z is an honest
  platform fallback only.
- **Why:** Office.js at WordApi 1.3 exposes no undo/undo-grouping API, so a
  snapshot restore is the only reliable mechanism; also avoids the prior
  destructive reverse-conversion bug (do-not-repeat H6). Confirmed by Spike C.

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
