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
- **Status:** Decided (Phase 2; confirmed + contracts refined in Phase 3)
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
- **Status:** Decided; revert mechanism **provisional pending Spike C**
- **Date:** 2026-06-22
- **Owner:** Agent (technical)
- **Decision:** "Revert Mukti changes" restores a pre-apply snapshot (text +
  per-run formatting) of the changed runs, stored as a **CustomXML part** in the
  .docx (document settings are too small — Phase 3 review). Ctrl+Z is an honest
  platform fallback only. Before reverting, the host checks the document still
  matches the snapshot and warns if it was edited (avoids silent data loss).
- **Why:** Office.js at WordApi 1.3 exposes no undo/undo-grouping API, so a
  snapshot restore is the only reliable mechanism; also avoids the prior
  destructive reverse-conversion bug (do-not-repeat H6). Confirmed by Spike C.

### D-0005 — Canonical Unicode output font
- **Status:** Decided · **Owner:** Maintainer (GRU953) · 2026-06-22
- **Decision:** Bundle **Noto Sans Bengali** (SIL OFL) as the single canonical
  output font; retain `OFL.txt`/NOTICE. **Self-hosted** (see D-0012), never loaded
  from Google by name.
- **Why:** Maintainer's choice; widest coverage, OFL, the spec's example. Does
  not affect accuracy (output codepoints are font-independent); changeable later
  at the cost of re-bundling + re-test.

### D-0006 — v1.0 ships on a custom domain (hard rule)
- **Status:** Decided (Phase 3); domain **name** still to be picked by maintainer
- **Owner:** Maintainer (GRU953)
- **Decision:** v1.0 publishes **only** on a custom domain (CNAME → GitHub Pages),
  so hosting can move without forcing re-sideload. GitHub Pages URL is used for
  development only. Launching on the raw `github.io` URL is **not** allowed
  (it would force every user to re-install later).
- **Open sub-item:** the maintainer picks/registers the domain name before
  release (a screenshot-level DNS guide will be in `RUNBOOK.md`).

### D-0012 — Strict CSP + self-hosted font (privacy)
- **Status:** Decided (Phase 3) · **Owner:** Agent (technical)
- **Decision:** Ship a real Content-Security-Policy: `script-src` limited to the
  Microsoft Office.js CDN + `'self'`; `font-src 'self'` (no Google Fonts);
  `connect-src 'none'` (no network calls). CI asserts the CSP exists. The Noto
  font is bundled as self-hosted woff2.
- **Why:** Phase 3 security review — the privacy claim needs enforcement, and
  loading the font from Google by name would beacon to Google on every open.

### D-0013 — Mapping-data provenance (clean, attributed)
- **Status:** Decided (Phase 3) · **Owner:** Agent (technical)
- **Decision:** Shipped `data/` is authored from the maintainer's own prior
  **MIT** Mukti map (retained with MIT attribution) + cited Unicode/MS specs. The
  unlicensed third-party converter is a disposable cross-check oracle only; no
  code copied. "Clean-room" is corrected to "clean-room except the maintainer's
  own MIT data".
- **Why:** Phase 3 security review (oracle has no licence).

### D-0014 — Accuracy is corpus-level CER; the gate is "≥99% on corpus vN"
- **Status:** Decided (Phase 3) · **Owner:** Agent (technical)
- **Decision:** The harness reports a **corpus-level** error rate (Σ edit-distance
  / Σ length), not a per-case mean. The release claim is "≥99% on the frozen
  corpus vN", **never** "99% accurate". The held-out set is small and
  same-author; treated as a sanity check, not a statistical guarantee — stated
  honestly in the known-limitations doc.
- **Why:** Phase 3 architecture/red-team review (avoids the prior H2 over-claim).

### D-0015 — Distribution path for non-technical end users
- **Status:** Decided (Phase 3) · **Owner:** Agent (technical) + Maintainer
- **Decision:** Lead the install guide with **Word-on-the-web → "Upload My
  Add-in"** (the lowest-friction path); document **Microsoft 365 centralized
  deployment** as the org route; name **AppSource** as the eventual endgame.
  Sideload friction (R8) is re-rated **High** impact, not "accepted".
- **Why:** Phase 3 product review — unsigned sideload is an adoption-killer for
  non-technical users.

### D-0016 — Spikes A, C and D gate the v1.0 release (and inform the build)
- **Status:** Decided (Phase 3) · **Owner:** Maintainer (runs) + Agent (gates)
- **Decision:** Spikes A (per-run font), C (snapshot revert) and the new **D
  (encoding seam — does Word's `Range.text` return the CP1252 code points the
  corpus assumes?)** must be **GREEN before the v1.0 release**. Per the
  maintainer's choice (answer "3a"), the **Phase 4 build proceeds in parallel**
  rather than waiting. The host contract is marked *provisional* and the engine's
  glyph map is keyed to the high-confidence CP1252 assumption. **Spike D is
  strongly recommended early** — a RED result means input-layer rework, so it is
  the cheapest 15 minutes the maintainer can spend. RED on any spike triggers the
  conservative fallback (selection-only / report-don't-convert) and a scope revision.
- **Why:** Phase 3 red-team — the encoding assumption underpins everything and is
  untested through Word; balanced against the maintainer's "build now" choice.

---

## Open decisions (need maintainer input)

- **D-0006 sub-item:** pick/register the custom domain name before v1.0 release.
- That is the only outstanding maintainer decision; everything else is decided.
