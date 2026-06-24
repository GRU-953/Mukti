# Do-Not-Repeat List

This is the forensic root-cause output of Phase 0: the concrete mistakes found
in the five prior "Mukti" versions, ranked by severity, each with a
plain-language explanation, why it matters, and what we will do instead. Every
item links to the detailed evidence under [`findings/`](findings/).

**How severity works:** 🔴 Critical = security/trust failure, fix before
anything else. 🟠 High = would break the product's core promise. 🟡 Medium =
quality/maintainability problem. 🟢 = something the prior versions actually got
right, kept here so we don't accidentally lose it.

---

## 🔴 Critical

### C1. A real, usable code-signing private key was committed to the repo
**Plain language:** The "digital signature" key used to sign the installers —
the thing that proves a download really came from Mukti — was saved directly
inside every version of the project, with no password protection. Worse, the
password for the related `.p12` key bundle was *written in plain text* in a
README right next to it.

**Evidence:** `signing/mukti-codesign.key` is an unencrypted RSA-4096 private
key whose fingerprint matches the committed certificate (so it is the live
signing key); `signing/mukti-codesign.p12` is a real key bundle whose password
was printed in `signing/README.md`; `certs/localhost.key` is also a committed
private key. ([findings/04-infra.md](findings/04-infra.md), §Secrets.)

**Why it matters:** Anyone who ever saw those files can sign software *as
Mukti*. Those credentials must be treated as **permanently compromised**.

**What we do instead:**
- Treat the old signing key/cert and the password as **burned** — never reuse
  them anywhere.
- This new repository starts clean; **no key, certificate, or password is ever
  committed.** `.gitignore` blocks `*.key`, `*.p12`, `*.pem`, `certs/`,
  `signing/`. A secret-scanning check runs in CI.
- Development TLS certificates are **generated on the developer's machine at
  build time**, never stored in the repo.
- Per the project rules, the maintainer (you) holds any real credentials; the
  automated pipeline never holds secrets and never self-publishes.

---

## 🟠 High

### H1. The output font is proprietary ("Kohinoor Bangla")
**Plain language:** When the old add-in converted text, it re-tagged it to a
font called **Kohinoor Bangla** — a commercial Apple/Linotype font. It was never
bundled (so on Windows it usually wasn't even installed, and text fell back to
some other font), and shipping a FOSS tool that depends on a paid font is a
licensing problem.

**Evidence:** `TARGET_UNICODE_FONT = "Kohinoor Bangla"` appears across the Word,
Excel, PowerPoint, and command code plus the UI and manifest; no font file is
bundled anywhere in any version. ([findings/04-infra.md](findings/04-infra.md),
§Licensing.)

**What we do instead:** Pick **one** openly-licensed (SIL OFL) Unicode Bangla
font, **bundle it**, and ship its licence/NOTICE file. (Default candidate: Noto
Sans Bengali; SolaimanLipi is the main alternative — this is a decision for the
maintainer, recorded in the decision log.)

### H2. The "100% accuracy across 47 documents" claim is not real
**Plain language:** The biggest selling point of the old versions can't be
trusted. The "test" that produced it (a) ran against 47 **private documents
that were never part of the project** and can't be re-checked by anyone, (b)
wasn't even run by the normal test command, and (c) **didn't measure accuracy at
all** — it counted a paragraph as "passing" if the output merely contained some
Bengali letters and didn't crash. Garbled-but-Bengali output scored as a pass.

**Evidence:** `bulk-docx-test.js` reads a `docs/` folder above the repo root
that isn't committed; its scoring only checks "contains a Bengali codepoint and
no leftover high bytes"; there is no comparison against expected text.
([findings/05-tests.md](findings/05-tests.md).)

**What we do instead:** Build and **freeze a versioned gold-standard corpus
first** — synthetic or properly licensed, **no private documents**, with a
held-out slice no agent ever sees — and measure real **character accuracy, word
accuracy, and formatting fidelity** against known-correct expected output. Gate
the release at ≥99% per-font and overall.

### H3. The conversion engine is not idempotent and has correctness bugs
**Plain language:** Running the conversion twice corrupts text (e.g. `কিছু`
becomes `কছিু`). It also never normalises its output to a standard Unicode form
(NFC), and the conversion of the Bengali full stop "dari" (।) is **silently
broken** by a text-escaping bug, so it never actually happens.

**Evidence:** the rearrange step re-fires on its own output; `normalize()`
appears nowhere; a double-escaped pattern (`\|`→`।`) never matches; some letters
are mis-classified (the constant named `NUKTA` actually points at a different
character). ([findings/01-engine.md](findings/01-engine.md), top-5 risks.)

**What we do instead:** Rewrite the engine in TypeScript with (a) a single,
ordered, longest-match pass, (b) **guaranteed NFC output**, (c) an
**idempotency test** (convert twice == convert once) in the gate, and (d) inputs
and outputs that never share characters so a second pass can't corrupt good
Unicode.

### H4. Unknown fonts are silently mangled instead of failing loudly
**Plain language:** If the add-in met a font it didn't recognise, it **assumed
it was Bijoy and converted it anyway**, quietly producing garbage. It also used
"fuzzy" name-matching that mistook unrelated fonts (any name ending in "MJ") for
Bijoy.

**Evidence:** `getEncodingFamily(font) || BIJOY` defaults unknown fonts to
Bijoy; `getEncodingFamily` classifies any `*mj/*cmj/...` suffix as Bijoy
(false positives like `NikoshMJ`, `XSutonnyOMJ`).
([findings/02-fonts.md](findings/02-fonts.md).)

**What we do instead:** A **defined known-font list**. Anything not on it is
reported as **"unsupported font"** and left untouched — a loud, visible failure,
never a silent edit. No fuzzy suffix guessing.

### H5. Per-paragraph (not per-run) font reading drops mixed-font text
**Plain language:** The old code read just one font name per paragraph. If a
paragraph mixed a Bijoy span with an English span, Word returned "no single
font," so the whole paragraph was **silently skipped** — real Bijoy text left
unconverted with no warning.

**Evidence:** reads a single `paragraph.font.name`; never iterates runs; mixed
paragraphs return `null` and are skipped.
([findings/03-officejs.md](findings/03-officejs.md).)

**What we do instead:** This is exactly **Spike #1** (per-run font access). We
will prove we can read the font of each *run* within a paragraph, and report any
region we genuinely can't scan rather than dropping it.

### H6. "Undo" was a destructive re-conversion, and there was no preview
**Plain language:** The old "undo" didn't restore the original — it ran the
conversion *backwards* over the whole document, which is lossy and changes
fonts. And there was no real preview: clicking convert changed your document
immediately.

**Evidence:** undo path is a reverse re-conversion; a `lastConversionState`
variable was declared but never used; conversion mutates the document with no
preview step. ([findings/03-officejs.md](findings/03-officejs.md).)

**What we do instead:** A real **Preview** before anything changes, and a
reliable **"Revert Mukti changes"** command. Edits go through Word's native undo
stack; we will not promise byte-identical OOXML, and we will be honest that the
number of Ctrl+Z presses is Word-determined. This is **Spike #3** (undo
fidelity).

---

## 🟡 Medium

### M1. Scope creep buried the actual product
Across versions the project grew Excel + PowerPoint support, a Bengali grammar
checker (20 rules), a proofreader, a 454K-word dictionary, reverse
Unicode→Bijoy, a "learning store," and word-by-word language detection — while
the core converter never improved. The MVP is **Bijoy→Unicode in Word, only.**
Everything else is explicitly deferred or cut.
([findings/01-engine.md](findings/01-engine.md), [02](findings/02-fonts.md).)

### M2. The manifest can't ship and doesn't declare its API needs
It targets three apps (Word/Excel/PowerPoint), declares **no `<Requirements>`
WordApi set at all**, points every URL at `https://localhost:3000`, and uses a
placeholder add-in Id. The forensic check confirms nothing it does needs more
than **WordApi 1.3**. ([findings/04-infra.md](findings/04-infra.md),
[03](findings/03-officejs.md).)

**What we do instead:** Word-only manifest, `<Requirements>` pinned to **WordApi
1.3**, a real **custom domain** (so hosting can move without forcing everyone to
re-install), and a fresh unique Id.

### M3. Performance: ~3 server round-trips per paragraph
The Word path called `context.sync()` about three times *per paragraph*
(~3000+ round-trips for a 1000-paragraph document). The Excel path (a couple of
syncs per 50-row batch) is the sane model.
([findings/03-officejs.md](findings/03-officejs.md).)

**What we do instead:** Budget performance **per `context.sync()`**, batch
reads/writes, and set targets from a calibration spike rather than guessing.

### M4. The 454K-word dictionary has no provenance or licence
A large word list shipped with no source, attribution, or licence — an
unresolved legal risk. It is also tied to out-of-scope proofreading.
([findings/04-infra.md](findings/04-infra.md).)

**What we do instead:** It's out of MVP scope. If any word data is ever needed,
it must have a clear, compatible licence and recorded provenance.

### M5. Repo hygiene & reproducibility
Committed build binaries (`dist-pkg/*.dmg`, `*.zip`, ~3.5 MB), committed Python
`__pycache__`, builds via `npm install --legacy-peer-deps` (never `npm ci`), no
`engines`/`.nvmrc`, no CI, no Dependabot, no CVE gate, no checksums, and
`curl | bash` installers that disable certificate checks.
([findings/04-infra.md](findings/04-infra.md), [05](findings/05-tests.md).)

**What we do instead:** No build artifacts in git; pinned toolchain + frozen
lockfile + `npm ci`; published artifact **checksums**; GitHub Actions
build/lint/test; Dependabot; a CVE gate on shipped dependencies.

### M6. Silent skipping with no transparency
Errors were swallowed by empty `catch {}` blocks and a single "skipped" counter
that lumped together wrong-font, empty, and unreadable content. The UI counter
meant to show font-skips was dead code.
([findings/03-officejs.md](findings/03-officejs.md),
[02](findings/02-fonts.md).)

**What we do instead:** Out-of-scope regions (footnotes, text boxes, comments,
fields, SmartArt) are reported as **"not scanned,"** never silently dropped.

---

## 🟢 What the prior versions got right (don't lose these)

- **G1. Privacy is genuinely clean.** No telemetry or analytics; the only
  external code load is Microsoft's legitimate Office.js CDN; learned data
  stayed in local storage; the bundled dev server makes no outbound calls. The
  "content never leaves your device" claim holds.
  ([findings/04-infra.md](findings/04-infra.md), §Privacy.)
- **G2. The Bijoy→Unicode character map (~265 pairs) is a real asset** worth
  salvaging into clean, schema-validated static JSON — *after* independent
  re-verification against the corpus. ([findings/01-engine.md](findings/01-engine.md).)
- **G3. The curated ~138-name Bijoy font list** is worth adapting (minus the
  fuzzy matching). ([findings/02-fonts.md](findings/02-fonts.md).)
- **G4. The engine core is already pure/Node-runnable** (no Office.js imports in
  the conversion math) — which is exactly the architecture the new spec
  mandates. ([findings/01-engine.md](findings/01-engine.md).)
- **G5. The Excel batching pattern** (few syncs per batch) is the performance
  model to emulate in Word. ([findings/03-officejs.md](findings/03-officejs.md).)

See [`../REUSE-MANIFEST.md`](../REUSE-MANIFEST.md) for the per-artifact
adopt/adapt/rewrite/discard verdicts.
