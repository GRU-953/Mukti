# Release checklist — the manual gate before publishing v1.0

This is the human gate that sits in front of the v1.0 release. The robots check
the code automatically on every change; **this checklist is the things only a
person can confirm.** Tick every box. If any box can't be ticked, **do not
publish** — tell me which one and we'll sort it before release.

It's normal for this to take a quiet afternoon. None of it is hard; most of it is
"open Word, follow the steps, watch it do the right thing". When every box is
ticked, you publish using [§4 of the RUNBOOK](../RUNBOOK.md#4-making-a-release).

> **Reassurance:** every check here is something you can see with your own eyes.
> Nothing relies on trusting a number you can't verify. A failed check is good
> news caught early — it means the gate is doing its job.

---

## A. The three Word spike kits are GREEN (D-0016)

The foundation tests must pass on **both** desktop Word and Word-on-the-web. (A
"spike kit" is a small read-only test; see
[§1 of the RUNBOOK](../RUNBOOK.md#1-run-the-word-test-kits-spikes-d-a-c).)

- [ ] **Spike D (encoding)** is GREEN on **Word on desktop**.
- [ ] **Spike D (encoding)** is GREEN on **Word on the web**.
- [ ] **Spike A (per-run font)** is GREEN on **Word on desktop**.
- [ ] **Spike A (per-run font)** is GREEN on **Word on the web**.
- [ ] **Spike C (revert)** is GREEN on **Word on desktop**.
- [ ] **Spike C (revert)** is GREEN on **Word on the web**.

*(That's 6 ticks: three kits × two places. All six must be green before v1.0 —
this is the hard gate from decision D-0016.)*

---

## B. End-to-end acceptance test (do this yourself, in real Word)

This is the whole product working start to finish, on a real document. Do it with
the version of Mukti you are about to publish, installed by following
[`INSTALL.md`](../INSTALL.md).

> **Before you start:** make a copy of your test document and keep it safe. This
> test changes a document on purpose.

1. Open a **real Bijoy/SutonnyMJ `.docx`** in Word. (If you don't have one, type a
   few words in the **SutonnyMJ** font — the old way of typing Bangla.)
2. On the **Home** tab, click the **Mukti** button. The Mukti panel opens on the
   right.
3. Click **Scan**. Mukti looks through the document for old Bijoy/SutonnyMJ text.
   - [ ] **Scan** finds the Bijoy text and reports it (nothing has changed yet).
4. Look at the **Preview** Mukti shows.
   - [ ] The preview **looks right** — the Bangla reads correctly, not as
     gibberish or jumbled letters. (Compare a word or two against the original.)
5. Click **Apply**.
   - [ ] The text in the document **converts to Unicode** and now displays in
     **Noto Sans Bengali** (the bundled modern font).
   - [ ] Any English or non-Bijoy text in the document is **left untouched**.
6. Click **Revert Mukti changes**.
   - [ ] The document is **restored** to exactly how it looked before Apply.

- [ ] **All six sub-checks above passed**, on a real document, in real Word.

---

## C. Privacy acceptance test (prove nothing leaves the device)

Mukti's core promise is that **your document content never leaves your computer**
(decision D-0012: the add-in is allowed to load its program code from Microsoft's
Office.js service and from the project's own hosting, and nothing else). This test
proves it with your own eyes using a "network monitor" — a free tool, built into
your browser, that lists everything the page contacts.

1. Use **Word on the web** in your browser (this makes the network easy to watch).
2. Before opening Mukti, open the browser's developer tools: press **F12** (or
   right-click the page → **Inspect**), then click the **Network** tab. *(This is
   the network monitor — it logs every address the page talks to.)*
3. Tick **Preserve log** (so the list isn't cleared) and leave it open.
4. Now open Mukti and run a full **Scan → Preview → Apply → Revert** on a test
   document (as in section B).
5. Look down the **Network** list at the addresses (the "Domain" or "Name"
   column). Confirm:
   - [ ] The only outside addresses contacted are **Microsoft's Office.js service**
     (addresses on `appsforoffice.microsoft.com` / `*.office.com` / `*.office.net`)
     and **the project's own hosting** (`add-in.yourdomain.org`, your custom
     domain). These are code-loads — fetching the add-in's own program files.
   - [ ] There is **no request that carries your document's text** to anywhere.
     (Your converted Bangla, file name, or document content never appears being
     sent out.)
   - [ ] There are **no analytics, tracking, telemetry or "fonts.googleapis.com"**
     requests at all.

- [ ] **Privacy test passed:** only the expected code-loads happened; no document
  content left the device.

*(If you see any address you don't recognise, stop and send me the list — don't
publish.)*

---

## D. Manifest, install and docs

- [ ] **The manifest is on the custom domain.** Download `manifest.xml` from the
  release and open it in a text editor (Notepad / TextEdit). The web addresses
  inside it point at **`https://add-in.yourdomain.org`** (your domain), **not** at
  `gru-953.github.io`. *(This is decision D-0006 — shipping on the raw GitHub
  address is not allowed, because it would force everyone to re-install later.)*
- [ ] **INSTALL.md tested on a clean machine.** On a computer (or account) that has
  **never** had Mukti, follow [`INSTALL.md`](../INSTALL.md) word for word and
  confirm Mukti installs and the **Mukti** button appears. (A "clean machine" just
  means a fresh starting point, so you're testing the real first-time experience.)
- [ ] **KNOWN-LIMITATIONS.md reviewed.** Read it through; confirm it honestly
  states what Mukti does and doesn't do (e.g. revert can't recover data if you
  saved/edited after converting; accuracy is "≥99% on the frozen test corpus", not
  "99% accurate"). Nothing over-claims.
- [ ] **Decision log signed off.** Skim [`DECISION-LOG.md`](DECISION-LOG.md) and
  confirm there are no decisions still marked **Open** or **Proposed** that block
  release. *(The one to watch: D-0006's open sub-item — you must have picked and
  pointed the custom domain, which section D above re-confirms.)*

---

## E. Automatic gates are all green

These are checked by the robots, but tick them here to confirm you've looked.

- [ ] **All CI checks green.** On the latest commit on `main` (repo → **Actions**
  tab), the **verify** run shows a **green tick**. (This single run covers lint,
  the engine-purity rule, unit tests and the accuracy gate.)
- [ ] **Corpus gate 100%.** The `verify` run's corpus step passed — the engine met
  the ≥99% accuracy gate on **both** the visible and the held-out test sets, with
  zero idempotency, NFC or no-op failures, and the fuzz test never threw. *(The
  "corpus" is the frozen set of known-correct conversions Mukti is graded
  against.)*
- [ ] **`npm audit` clean / CVE gate green.** No high or critical security
  vulnerability ("CVE" = a publicly catalogued security flaw) in any building block
  that actually ships to users. (The security step in CI confirms this; a green CI
  run means it passed.)

---

## When every box is ticked

1. Tell me everything above is green — I'll do a final confirmation pass with you.
2. Publish using [§4 of the RUNBOOK](../RUNBOOK.md#4-making-a-release) (create the
   `v1.0.0` tag / draft the release).
3. Watch the **release** run in the **Actions** tab finish with a green tick, and
   confirm the published site loads at your custom domain.

That's the release. Calm, checked, and reversible — exactly how we want it.
