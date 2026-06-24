# Glossary (plain language)

Every technical term used in this project, explained simply. This grows as the
project does. If you ever hit a word that isn't here, that's a bug in *this
file* — tell me and I'll add it.

## Bangla / typography

- **Bijoy** — A popular *legacy* way of typing Bangla from before Unicode was
  common. It stores Bangla by reusing the slots meant for English letters and
  symbols, so the bytes only look like Bangla when a matching Bijoy font is
  applied. Copy that text anywhere else and it turns to gibberish. Converting it
  to Unicode is what Mukti does.
- **SutonnyMJ** — The most common Bijoy-family font. "Bijoy/SutonnyMJ" basically
  means "the old way."
- **ANSI / legacy / 8-bit encoding** — The general name for that old "reuse the
  English slots" approach Bijoy belongs to.
- **Unicode** — The modern, universal standard where every character has one
  fixed identity. Unicode Bangla works in any app, any font, any device. This is
  the *output* Mukti produces.
- **Code point** — Unicode's ID number for a single character.
- **NFC (Normalisation Form C)** — Unicode often lets the *same* text be stored
  more than one way. NFC is the standard "tidied-up" form. Mukti always outputs
  NFC so its results are consistent and searchable.
- **Conjunct (যুক্তাক্ষর)** — Two or more consonants joined into one shape (e.g.
  ক + ্ + ত → ক্ত). Getting these right is the hard part of Bangla conversion.
- **Hosonto / virama (্)** — The mark that joins consonants into a conjunct.
- **Reph (র্)** — The little mark for an "r" sound that *visually* sits on top of
  the following letter but, in Bijoy, is often typed *before* it. Reordering it
  correctly is a classic source of bugs.
- **Pre-base vowel (ি, ে, ৈ)** — Vowel signs that are *drawn* to the left of
  their consonant but, in Unicode, are *stored* after it. Bijoy often stores them
  in drawing order, so they must be reordered on conversion.
- **Ya-phala / vowel signs** — Other attached marks that also need careful
  ordering.
- **Dari (।)** — The Bangla full stop. (The old engine's dari conversion was
  silently broken — see the do-not-repeat list.)

## Office add-in

- **Office Add-in** — A small web app that runs *inside* Microsoft Word and can
  read/change your document. Mukti is one of these.
- **Office.js** — Microsoft's official JavaScript library that an add-in uses to
  talk to Word. It is always loaded from Microsoft's servers (never bundled).
- **Manifest** — A small XML file that tells Word what the add-in is, where its
  code lives, and which buttons to show. Installing/changing it is "sideloading."
- **Sideload** — Installing an add-in from a manifest file directly, rather than
  from Microsoft's store. Shows an "unknown developer" warning, which we'll
  document.
- **Taskpane** — The panel that opens on the side of Word where Mukti's buttons
  and preview live.
- **Ribbon** — The toolbar across the top of Word; we add a Mukti button to it.
- **WordApi requirement set** — A version number for the set of Word features an
  add-in needs (e.g. "WordApi 1.3"). Declaring the *lowest* version that works
  means Mukti runs on the widest range of Word versions. We target **1.3**.
- **`context.sync()`** — The moment the add-in actually sends its queued
  read/write commands to Word and waits for a reply. Each one is a round-trip, so
  doing too many (e.g. one per paragraph) is slow. We budget performance by
  counting these, not by counting words.
- **Run** — A stretch of text within a paragraph that all shares the same
  formatting (same font, bold, etc.). One paragraph can contain several runs.
- **CDN (Content Delivery Network)** — Servers that deliver program code quickly.
  Office.js comes from Microsoft's CDN. Loading code from a CDN is normal and
  expected; it does **not** mean your document content is sent anywhere.
- **OOXML** — The XML format Word documents are made of internally. We preserve
  *formatting* but do not promise the underlying OOXML is byte-for-byte identical.

## Engineering / process

- **Idempotent** — "Doing it twice gives the same result as doing it once."
  Converting an already-converted document must not corrupt it. The old engine
  failed this; the new one is tested for it.
- **Clean-room** — Rebuilding from scratch rather than reusing old code, except
  for specific pieces that have *earned* their place by passing tests.
- **Gold-standard corpus** — A frozen, version-controlled set of test documents
  with known-correct expected output, used to measure accuracy honestly.
- **Held-out slice** — A portion of the corpus that no developer (human or AI)
  is allowed to see while building, so the final accuracy score can't be "gamed."
- **Character / word accuracy** — Percentage of characters / whole words the
  converter gets exactly right versus the known-correct answer.
- **Formatting fidelity** — How faithfully bold, italic, size, colour, and
  paragraph structure survive the conversion.
- **Spike** — A small, time-boxed experiment to answer one risky question before
  committing to a design. A "red" spike means the answer was bad and the plan
  must change.
- **TypeScript** — A safer version of JavaScript that catches mistakes before the
  code runs. The conversion engine will be pure TypeScript.
- **Lint / linter** — An automated style/safety checker for code. We use one to
  *enforce* that the engine never imports Office.js.
- **Lockfile / `npm ci`** — A file pinning the exact versions of every
  dependency, plus the command that installs *exactly* those versions, so builds
  are reproducible.
- **Checksum** — A short fingerprint of a file; publishing it lets users verify a
  download wasn't tampered with.
- **CVE** — A publicly catalogued software security vulnerability. A "CVE gate"
  blocks shipping if a serious, reachable one is found in our dependencies.
- **Dependabot** — A GitHub bot that opens pull requests to update dependencies
  (e.g. to fix security issues).
- **CI / GitHub Actions** — Automation that runs the build, lint, and tests
  automatically every time code changes.
- **Code signing** — Attaching a cryptographic signature to software so users can
  verify who made it. The *private key* for this must be kept secret (the old
  versions leaked theirs — see the do-not-repeat list).
- **SIL OFL (Open Font License)** — A licence that lets fonts be freely used,
  bundled, and redistributed. Our bundled output font will be OFL.
- **Sideload "unknown developer" friction** — Because we won't pay for store
  publishing, Word shows a warning when installing Mukti. We'll explain it
  honestly in the install guide.
- **Rollback** — A documented way to undo a step if it goes wrong.
