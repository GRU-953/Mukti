# Mukti — UI / UX Design (Phase 2, design only)

**Status:** Design — *no implementation in this phase.*
**Scope:** The taskpane UI for the MVP: one-click **whole-document** Bijoy/SutonnyMJ → Unicode
conversion in Microsoft Word, with **Preview before apply** and a reliable
**"Revert Mukti changes"** command.

This document is implementation-agnostic (no framework code) but concrete enough to build from
without further questions. It builds on, and does not repeat:

- [`../phase0/DO-NOT-REPEAT.md`](../phase0/DO-NOT-REPEAT.md) — the failures we must not recreate.
- [`../DECISION-LOG.md`](../DECISION-LOG.md) — frozen decisions (Word-only MVP, Noto Sans Bengali,
  loud unsupported-font handling, real preview + revert, transparency on out-of-scope regions).
- [`../GLOSSARY.md`](../GLOSSARY.md) — plain-language definitions (Bijoy, run, taskpane, etc.).

### How this UI directly answers the Phase 0 findings

| Finding | UI consequence in this design |
| --- | --- |
| H4 — unknown fonts silently mangled | A dedicated **Unsupported-font warning** state that lists the offending fonts and refuses to convert them. Never a silent edit. |
| H5 — mixed-font paragraphs dropped | Reporting is **per run**, so a paragraph that mixes Bijoy + English shows both "to convert" and "left alone" honestly. |
| H6 — destructive "undo", no preview | A real **Preview** (before/after) gate before any document change, plus a first-class **Revert Mukti changes** command that restores a pre-apply snapshot (Ctrl+Z is an honest platform fallback). |
| M6 — silent skipping | A **"Not scanned" report** that names every out-of-scope region by category and count; nothing is silently dropped. |
| G1 — privacy is clean | A visible **privacy line** in About; online-first wording, never "offline". |

---

## 1. Information architecture of the taskpane

The taskpane is a single vertical panel. It is a **state machine with one screen**: a persistent
header and footer, and a body that swaps between states. Only one primary state is visible at a
time; secondary states (warning, not-scanned report) appear inline within or stacked under the
relevant primary state, never in a separate window.

### 1.1 Persistent chrome (always rendered)

```
HEADER  : [Mukti logo/wordmark] ............... [ভাষা: বাংলা ▾ / Language ▾ ]
BODY    : <state-specific region, role="main", aria-live="polite">
FOOTER  : [ℹ️ পরিচিতি / About]   ·   privacy one-liner (truncated, expands in About)
```

- The **language toggle** lives top-right of the header and is reachable on every state.
- The **About** affordance lives in the footer and is reachable on every state; it carries the
  privacy line and the Noto Sans Bengali / licence notice.
- The body region is an `aria-live="polite"` landmark so screen readers announce state changes
  (e.g. scan finished, conversion applied) without the user hunting for them.

### 1.2 State map

| # | State | Purpose | Entry from | Primary actions |
| --- | --- | --- | --- | --- |
| S0 | **Initial / Idle** | Fast cold-start screen; one clear call to action. | App load, or after Done/Reverted "start over" | **Convert document** |
| S1 | **Scanning** | Read the document per run, classify fonts & regions. No edits yet. | S0 Convert | Cancel |
| S2 | **Preview (before/after)** | Show what *will* change, with counts and a before→after sample. The decision gate. | S1 completes with ≥1 convertible run | **Apply**, Cancel |
| S2a | **Unsupported-font warning** (inline within S1→S2) | Loudly name Bangla-looking fonts not on the known list; exclude them from conversion. | S1 detects unknown fonts | Acknowledge, view list, continue to S2 |
| S2b | **Not-scanned report** (inline within S2) | Disclose out-of-scope regions (footnotes, text boxes, comments, fields, SmartArt = not scanned; headers/footers = pending). | Always shown in S2 if any exist | Expand/collapse details |
| S3 | **Applying** | Write conversions to the document; progress. | S2 Apply | (no cancel once writing — see §4) |
| S4 | **Done** | Confirm success with counts; offer Revert. | S3 completes | **Revert Mukti changes**, Start over |
| S5 | **Reverted** | Confirm the document was restored. | S4 Revert, or Revert command | Convert again, Start over |
| S6 | **Error** | Recoverable failure with plain-language cause + retry. | Any state on failure | Retry, Start over, Report-how (offline help) |
| E0 | **Empty / nothing to convert** | Scan found no supported Bijoy text. | S1 completes with 0 convertible runs | Start over; (shows not-scanned report if relevant) |

State transitions are summarised in §4. A document that is **all unsupported font** lands in
S2a → E0 (nothing to convert), not in Preview.

---

## 2. Wireframe sketches (ASCII)

> Wireframes are **layout/affordance sketches only** — not final visuals. Spacing, exact colours,
> and brand marks are out of scope here (see §6).

### 2.1 Initial / Idle (S0) — the fast cold-start screen

```
┌─────────────────────────────────────────────┐
│ মুক্তি  Mukti                 [ ভাষা: বাংলা ▾ ] │  header
├─────────────────────────────────────────────┤
│                                               │
│   বিজয়/সুটনিএমজে লেখা ইউনিকোডে রূপান্তর করুন।        │  H1 (heading)
│   Convert Bijoy/SutonnyMJ text to Unicode.      │  (shown only in EN mode)
│                                               │
│   এই কাজটি পুরো নথিতে চলবে। পরিবর্তন প্রয়োগের      │  helper text
│   আগে আপনি প্রিভিউ দেখতে পাবেন।                 │
│                                               │
│        ┌───────────────────────────────┐      │
│        │   নথি রূপান্তর করুন             │      │  PRIMARY button
│        │   Convert document            │      │  (autofocus)
│        └───────────────────────────────┘      │
│                                               │
│   ⓘ পরিবর্তন প্রয়োগের আগে কিছুই বদলাবে না।       │  reassurance
│      Nothing changes until you apply.         │
│                                               │
├─────────────────────────────────────────────┤
│ ℹ️ পরিচিতি / About · আপনার লেখা ডিভাইস ছাড়ে না   │  footer + privacy
└─────────────────────────────────────────────┘
```

The Idle screen is intentionally static text + one button so it can paint within the
**≤2 s cold-start budget**. Office.js readiness, font-list load, and engine init happen
*after* this screen is shown; the Convert button is disabled with a brief "প্রস্তুত হচ্ছে…/
Getting ready…" inline note until the engine signals ready, then enables (focus is preserved).

### 2.2 Scanning (S1)

```
┌─────────────────────────────────────────────┐
│ মুক্তি  Mukti                 [ ভাষা: বাংলা ▾ ] │
├─────────────────────────────────────────────┤
│   নথি স্ক্যান করা হচ্ছে…                         │  status (aria-live)
│   Scanning your document…                     │
│                                               │
│   [▓▓▓▓▓▓▓░░░░░░]  অনুচ্ছেদ ১২৪ / ৪১০            │  progress (determinate
│                    Paragraph 124 / 410        │   if known; else label
│                                               │   "indeterminate")
│   কোনো পরিবর্তন এখনো করা হয়নি।                   │
│   No changes have been made yet.              │
│                                               │
│            [ বাতিল / Cancel ]                  │
├─────────────────────────────────────────────┤
│ ℹ️ পরিচিতি / About                              │
└─────────────────────────────────────────────┘
```

### 2.3 Preview (S2) — the decision gate, with inline warning + not-scanned report

```
┌─────────────────────────────────────────────┐
│ মুক্তি  Mukti                 [ ভাষা: বাংলা ▾ ] │
├─────────────────────────────────────────────┤
│   প্রিভিউ — যা পরিবর্তন হবে                       │  H1
│   Preview — what will change                  │
│                                               │
│   ✔ রূপান্তরযোগ্য: ৩৮২ অংশ (১২,৪১০ অক্ষর)         │  summary counts
│     Convertible: 382 runs (12,410 chars)      │  (icon + text, not
│   • অপরিবর্তিত (ইউনিকোড/ইংরেজি): ৯০ অংশ           │   colour-only)
│     Left as-is (Unicode/English): 90 runs     │
│                                               │
│  ┌── নমুনা — আগে → পরে ──────────────────────┐ │  before/after sample
│  │ আগে (বিজয়):   Avwg evsjvq wjwL           │ │  "before" = raw bytes
│  │ Before (Bijoy)                           │ │  rendered in the doc's
│  │ ─────────────────────────────────────── │ │  original Bijoy font
│  │ পরে (ইউনিকোড): আমি বাংলায় লিখি          │ │  "after" = Unicode in
│  │ After (Unicode)                          │ │  Noto Sans Bengali
│  │            [ পরবর্তী নমুনা ▸ / Next ▸ ]   │ │  (cycle a few samples)
│  └──────────────────────────────────────────┘ │
│                                               │
│  ⚠ অসমর্থিত ফন্ট পাওয়া গেছে (২টি)  [ দেখুন ▾ ]   │  S2a inline warning
│     Unsupported font found (2)   [ View ▾ ]   │  (collapsed; see 2.4)
│                                               │
│  ◔ যা স্ক্যান করা হয়নি (৩টি ধরন)  [ দেখুন ▾ ]    │  S2b not-scanned
│     Not scanned (3 kinds)        [ View ▾ ]   │  (collapsed; see 2.5)
│                                               │
│   ┌──────────────────┐  ┌──────────────────┐  │
│   │ প্রয়োগ করুন        │  │ বাতিল / Cancel    │  │  Apply (primary, focus)
│   │ Apply            │  │                  │  │  Cancel (secondary)
│   └──────────────────┘  └──────────────────┘  │
├─────────────────────────────────────────────┤
│ ℹ️ পরিচিতি / About                              │
└─────────────────────────────────────────────┘
```

The **before** sample is deliberately shown using the document's *original Bijoy font* so the
user recognises their own text; the **after** sample is rendered in **Noto Sans Bengali** (the
bundled output font) so the preview matches the applied result. Each is labelled in text.

### 2.4 Unsupported-font warning expanded (S2a)

```
│  ⚠ অসমর্থিত ফন্ট পাওয়া গেছে (২টি)  [ লুকান ▴ ]   │
│     Unsupported font found (2)   [ Hide ▴ ]   │
│  ┌──────────────────────────────────────────┐ │
│  │ এই ফন্টগুলো পরিচিত বিজয় তালিকায় নেই, তাই    │ │  explanation
│  │ নিরাপত্তার জন্য রূপান্তর করা হবে না:          │ │
│  │ These fonts are not on the known Bijoy     │ │
│  │ list, so they will NOT be converted:       │ │
│  │   • "BanglaFancyMJ"  — ৪১ অংশ / 41 runs    │ │  named, with counts
│  │   • "ProttoyOMJ"     — ৭ অংশ / 7 runs      │ │
│  │ এগুলো অপরিবর্তিত থাকবে। ভুল রূপান্তর এড়াতে    │ │  why we refuse
│  │ আমরা অনুমান করি না।                          │ │
│  │ They are left untouched — we never guess.  │ │
│  └──────────────────────────────────────────┘ │
```

If **every** Bijoy-looking run is unsupported, Apply is disabled and the panel routes to the
Empty state (E0) with this warning shown above it.

### 2.5 Not-scanned report expanded (S2b)

```
│  ◔ যা স্ক্যান করা হয়নি (৩টি ধরন)  [ লুকান ▴ ]    │
│     Not scanned (3 kinds)        [ Hide ▴ ]   │
│  ┌──────────────────────────────────────────┐ │
│  │ এই অংশগুলো এই সংস্করণে পরীক্ষা করা হয় না।    │ │
│  │ These regions are out of scope this        │ │
│  │ version and were NOT scanned:              │ │
│  │   • ফুটনোট / Footnotes ............ ৩       │ │  category + count
│  │   • টেক্সট বক্স / Text boxes ....... ১       │ │  (status="not scanned")
│  │   • মন্তব্য / Comments ............. ৫       │ │
│  │ হেডার/ফুটার: এই সংস্করণে মুলতবি।             │ │  headers/footers =
│  │ Headers/footers: pending this version.     │ │  "pending" (distinct)
│  │ মূল লেখা ও টেবিল স্ক্যান করা হয়েছে।          │ │  in-scope confirmation
│  │ Body text and tables were scanned.         │ │
│  └──────────────────────────────────────────┘ │
```

### 2.6 Done (S4)

```
│   ✔ রূপান্তর সম্পন্ন                              │  success heading
│   Conversion complete                         │  (icon + text)
│   ৩৮২ অংশ ইউনিকোডে রূপান্তরিত হয়েছে।             │  counts
│   382 runs converted to Unicode.              │
│   ⓘ ফন্ট সেট করা হয়েছে: Noto Sans Bengali        │
│      Output font set to: Noto Sans Bengali    │
│   ┌─────────────────────────────────────────┐ │
│   │  মুক্তির পরিবর্তন ফিরিয়ে নিন                │ │  PRIMARY (autofocus)
│   │  Revert Mukti changes                    │ │
│   └─────────────────────────────────────────┘ │
│   [ নতুন করে শুরু / Start over ]                 │  secondary
│   ⓘ Ctrl+Z দিয়েও ফেরানো যাবে (Word নিয়ন্ত্রিত)।  │  honest undo note
│      You can also use Ctrl+Z (Word decides    │  (per H6)
│      how many presses).                       │
```

---

## 3. Bilingual string table (Bangla is the default; English is the toggle)

All UI strings live behind a key. **Bangla is shown by default**; English appears when the user
flips the toggle (or when the toggle is set to "both" in mixed-mode wireframes above, the strings
are simply the two columns). Numerals: the UI renders Bangla-Indic digits (০১২৩…) in Bangla mode
and ASCII digits in English mode; the keys below show representative values.

| Key | বাংলা (default) | English |
| --- | --- | --- |
| `app.name` | মুক্তি | Mukti |
| `app.tagline` | বিজয়/সুটনিএমজে লেখা ইউনিকোডে রূপান্তর | Convert Bijoy/SutonnyMJ to Unicode |
| `lang.toggle.label` | ভাষা | Language |
| `lang.option.bn` | বাংলা | Bangla |
| `lang.option.en` | ইংরেজি | English |
| `idle.heading` | বিজয়/সুটনিএমজে লেখা ইউনিকোডে রূপান্তর করুন | Convert Bijoy/SutonnyMJ text to Unicode |
| `idle.helper` | এই কাজটি পুরো নথিতে চলবে। পরিবর্তন প্রয়োগের আগে আপনি প্রিভিউ দেখতে পাবেন। | This runs on the whole document. You will see a preview before any change is applied. |
| `idle.reassure` | পরিবর্তন প্রয়োগের আগে কিছুই বদলাবে না। | Nothing changes until you apply. |
| `btn.convert` | নথি রূপান্তর করুন | Convert document |
| `status.preparing` | প্রস্তুত হচ্ছে… | Getting ready… |
| `status.scanning` | নথি স্ক্যান করা হচ্ছে… | Scanning your document… |
| `scan.progress` | অনুচ্ছেদ {done} / {total} | Paragraph {done} / {total} |
| `scan.nochange` | কোনো পরিবর্তন এখনো করা হয়নি। | No changes have been made yet. |
| `btn.cancel` | বাতিল | Cancel |
| `preview.heading` | প্রিভিউ — যা পরিবর্তন হবে | Preview — what will change |
| `preview.count.convertible` | রূপান্তরযোগ্য: {runs} অংশ ({chars} অক্ষর) | Convertible: {runs} runs ({chars} chars) |
| `preview.count.asis` | অপরিবর্তিত (ইউনিকোড/ইংরেজি): {runs} অংশ | Left as-is (Unicode/English): {runs} runs |
| `preview.sample.label` | নমুনা — আগে → পরে | Sample — before → after |
| `preview.sample.before` | আগে (বিজয়) | Before (Bijoy) |
| `preview.sample.after` | পরে (ইউনিকোড) | After (Unicode) |
| `btn.sample.next` | পরবর্তী নমুনা | Next sample |
| `btn.apply` | প্রয়োগ করুন | Apply |
| `warn.unsupported.title` | অসমর্থিত ফন্ট পাওয়া গেছে ({n}টি) | Unsupported font found ({n}) |
| `warn.unsupported.body` | এই ফন্টগুলো পরিচিত বিজয় তালিকায় নেই, তাই নিরাপত্তার জন্য রূপান্তর করা হবে না: | These fonts are not on the known Bijoy list, so they will NOT be converted: |
| `warn.unsupported.item` | "{font}" — {runs} অংশ | "{font}" — {runs} runs |
| `warn.unsupported.why` | এগুলো অপরিবর্তিত থাকবে। ভুল রূপান্তর এড়াতে আমরা অনুমান করি না। | They are left untouched — we never guess. |
| `notscanned.title` | যা স্ক্যান করা হয়নি ({n}টি ধরন) | Not scanned ({n} kinds) |
| `notscanned.body` | এই অংশগুলো এই সংস্করণে পরীক্ষা করা হয় না এবং স্ক্যান করা হয়নি: | These regions are out of scope this version and were NOT scanned: |
| `notscanned.footnotes` | ফুটনোট | Footnotes |
| `notscanned.textboxes` | টেক্সট বক্স | Text boxes |
| `notscanned.comments` | মন্তব্য | Comments |
| `notscanned.fields` | ফিল্ড | Fields |
| `notscanned.smartart` | স্মার্টআর্ট | SmartArt |
| `notscanned.headerfooter` | হেডার/ফুটার: এই সংস্করণে মুলতবি। | Headers/footers: pending this version. |
| `notscanned.inscope` | মূল লেখা ও টেবিল স্ক্যান করা হয়েছে। | Body text and tables were scanned. |
| `btn.viewmore` | দেখুন | View |
| `btn.viewless` | লুকান | Hide |
| `status.applying` | পরিবর্তন প্রয়োগ করা হচ্ছে… | Applying changes… |
| `apply.progress` | {done} / {total} অংশ লেখা হচ্ছে | Writing {done} / {total} runs |
| `done.heading` | রূপান্তর সম্পন্ন | Conversion complete |
| `done.count` | {runs} অংশ ইউনিকোডে রূপান্তরিত হয়েছে। | {runs} runs converted to Unicode. |
| `done.font` | ফন্ট সেট করা হয়েছে: Noto Sans Bengali | Output font set to: Noto Sans Bengali |
| `btn.revert` | মুক্তির পরিবর্তন ফিরিয়ে নিন | Revert Mukti changes |
| `btn.startover` | নতুন করে শুরু | Start over |
| `done.undonote` | Ctrl+Z দিয়েও ফেরানো যাবে (কত বার চাপতে হবে তা Word ঠিক করে)। | You can also use Ctrl+Z (Word decides how many presses). |
| `status.reverting` | পরিবর্তন ফিরিয়ে নেওয়া হচ্ছে… | Reverting changes… |
| `reverted.heading` | পরিবর্তন ফিরিয়ে নেওয়া হয়েছে | Changes reverted |
| `reverted.body` | মুক্তির করা পরিবর্তনগুলো সরিয়ে নথি আগের অবস্থায় ফেরানো হয়েছে। | Mukti's changes were removed and the document was restored. |
| `btn.convertagain` | আবার রূপান্তর করুন | Convert again |
| `empty.heading` | রূপান্তরযোগ্য কিছু পাওয়া যায়নি | Nothing to convert |
| `empty.body` | মূল লেখা ও টেবিলে কোনো সমর্থিত বিজয় লেখা পাওয়া যায়নি। | No supported Bijoy text was found in the body or tables. |
| `error.heading` | কিছু একটা ভুল হয়েছে | Something went wrong |
| `error.body.generic` | রূপান্তর সম্পূর্ণ করা যায়নি। নথিতে কোনো পরিবর্তন করা হয়নি। | The conversion could not be completed. No changes were made to your document. |
| `error.body.applyfail` | পরিবর্তন প্রয়োগে সমস্যা হয়েছে। অনুগ্রহ করে নথিতে Ctrl+Z চাপুন এবং আবার চেষ্টা করুন। | Applying changes failed. Please press Ctrl+Z in the document and try again. |
| `btn.retry` | আবার চেষ্টা করুন | Try again |
| `about.title` | পরিচিতি | About |
| `about.privacy` | আপনার নথির লেখা কখনো আপনার ডিভাইস ছাড়ে না — কোনো লেখা, ফাইলের নাম বা মেটাডেটা পাঠানো হয় না; কোনো টেলিমেট্রি নেই। | Your document content never leaves your device — no text, filenames, or metadata are transmitted; no telemetry. |
| `about.online` | মুক্তি অনলাইন-ফার্স্ট: চালু হলে কোডটি মাইক্রোসফটের CDN ও প্রকল্পের হোস্টিং থেকে লোড হয়। (এটি "অফলাইন" নয়।) | Mukti is online-first: code loads from Microsoft's CDN and project hosting at launch. (This is not "offline".) |
| `about.font` | আউটপুট ফন্ট: Noto Sans Bengali (SIL OFL)। লাইসেন্স অ্যাপের সাথে দেওয়া আছে। | Output font: Noto Sans Bengali (SIL OFL). Licence is bundled with the app. |
| `about.version` | সংস্করণ {version} | Version {version} |

> **Note on `about.online`:** per the spec and DECISION-LOG, the copy must *not* claim "offline".
> It states the privacy promise (content stays local) **and** that program code is loaded online.

---

## 4. Interaction flows

### 4.1 Convert flow (scan → preview → apply)

```
S0 Idle
  └─[Convert document]→ S1 Scanning
        ├─ engine reads document per RUN (not per paragraph) — see H5
        ├─ classifies each run: convertible-Bijoy | already-Unicode/English (no-op)
        │                       | unsupported-Bijoy-looking-font
        ├─ tallies out-of-scope regions for the not-scanned report
        ├─[Cancel]→ S0 (no changes were ever made)
        └─ scan done →
              ├─ 0 convertible runs → E0 Empty (with S2a/S2b reports if any)
              └─ ≥1 convertible run → S2 Preview
                                        ├─ S2a unsupported-font warning (if any)
                                        ├─ S2b not-scanned report (if any)
                                        ├─[Cancel]→ S0 (no changes were ever made)
                                        └─[Apply]→ S3 Applying
                                                    ├─ snapshot original runs (text+formatting) FIRST
                                                    ├─ batched writes (few syncs/batch; M3/G5)
                                                    ├─ sets output font to Noto Sans Bengali
                                                    └─ done → S4 Done
                                                       │  (on failure → S6 Error)
```

Key promises encoded in the flow:

- **Nothing is written before Apply.** Scan and Preview are read-only; the panel says so.
- Apply **takes a snapshot of the original runs (text + formatting) before changing anything**,
  which is what makes Revert reliable (see §4.2). Each edit also lands on Word's native undo
  stack, but we do not promise an exact number of Ctrl+Z presses (H6).
- **Convertible vs. no-op classification** follows decision D-0007: a run with no Bijoy source
  glyphs is a no-op and is *reported as "left as-is"*, never silently touched.

### 4.2 Revert flow

```
S4 Done ─[Revert Mukti changes]→ S_reverting (transient) → S5 Reverted
                                     └─ on failure → S6 Error (with manual Ctrl+Z guidance)
```

- Revert restores the pre-conversion document by **re-writing the snapshot** taken at Apply time
  (the original text + each run's font/bold/italic/size/colour). It is **not** a reverse
  re-conversion (that was the H6 bug), and it does **not** rely on programmatic "undo" — Office.js
  at WordApi 1.3 has no undo API, so the snapshot is the reliable mechanism. The snapshot is stored
  in the document's settings, so Revert still works after the task pane is closed and reopened
  (see `src/host/contracts.ts` → `RevertSnapshot`, `latestSnapshotId`).
- **Ctrl+Z** remains available as a platform fallback; the Error state tells the user to try it,
  and the copy is honest that Word — not Mukti — owns the native undo stack and the number of
  presses varies by platform.
- After the user manually edits the converted text, a snapshot restore may overwrite those edits;
  the copy warns about this rather than over-promising.

### 4.3 Language-toggle flow

```
Any state ─[Language ▾]→ menu { বাংলা · English } ─[select]→ same state re-rendered in chosen lang
```

- Default is **Bangla**. The chosen language persists for the session (and across reopen via
  `Office.context` roaming/local settings — a *preference only*, never document content, so the
  privacy promise holds).
- Toggling language **does not change application state or lose progress** (e.g. mid-preview stays
  mid-preview). Focus is restored to the control the user was on (the toggle), and the live region
  announces the language change.
- The toggle control is itself bilingual (`ভাষা / Language`) so it is recognisable in either mode.

### 4.4 Keyboard navigation map

Every action is reachable and operable from the keyboard; nothing requires a pointer.

| Context | Tab order (logical) | Shortcuts / keys |
| --- | --- | --- |
| Global | Language toggle → state body controls → About | `Tab` / `Shift+Tab` move; `Esc` closes any open menu/expander and returns focus to its trigger |
| S0 Idle | Convert (autofocus) → About → Language | `Enter`/`Space` on Convert starts scan |
| S1 Scanning | Cancel (focusable) | `Enter`/`Space` cancels; `Esc` also cancels |
| S2 Preview | Apply (autofocus) → Cancel → Next-sample → Unsupported expander → Not-scanned expander → About → Language | `Enter`/`Space` activates focused control; expanders toggle with `Enter`/`Space`; `Esc` collapses an expanded section |
| S4 Done | Revert (autofocus) → Start over → About → Language | `Enter`/`Space` activates |
| S5 Reverted | Convert again (autofocus) → Start over → Language | — |
| S6 Error | Retry (autofocus) → Start over → About | — |
| Menus/expanders | Roving focus within the open menu | `↑`/`↓` move within menu; `Enter` selects; `Esc` closes |

- **Ribbon entry point:** the Word ribbon button opens the taskpane; on open, focus moves into the
  taskpane body (to the autofocus control of the current state). The add-in does **not** register
  global Word keyboard shortcuts in the MVP (deferred — see §8).
- **No keyboard trap:** focus can always leave any expander/menu via `Tab` or `Esc`.

### 4.5 Focus management rules

1. On each state change, move focus to that state's **primary action** (marked "autofocus" above),
   and announce the new state via the `aria-live="polite"` body region.
2. Opening an expander (`View ▾`) keeps focus on the trigger; the revealed content is inserted
   immediately after it in DOM/tab order. Collapsing returns focus to the trigger.
3. The language menu and About return focus to their trigger on close (`Esc` or selection).
4. Never move focus *to* a disabled control. While the engine is "Getting ready…", focus stays on
   the (disabled) Convert button so it activates the moment it enables.
5. Progress/transient states (Scanning, Applying, Reverting) keep focus on the only interactive
   control (Cancel where present) or on the status text if none.

---

## 5. Accessibility checklist → WCAG 2.1 AA

> Target: **WCAG 2.1 Level AA**. Each row names the success criterion (SC) and how this UI meets it.

| Area | Requirement (WCAG 2.1 AA) | How Mukti's UI meets it |
| --- | --- | --- |
| Contrast — text | **1.4.3** body/label text ≥ **4.5:1**; large text (≥18.66px bold / ≥24px) ≥ **3:1** | Token system in §6 mandates these ratios; verified per token pair, not by eyeballing. |
| Contrast — non-text | **1.4.11** UI components, focus rings, icons, progress bar ≥ **3:1** against adjacent colours | Focus ring and control borders specified ≥3:1 (see §6). |
| No colour-only signalling | **1.4.1** | Every status uses **icon + text + shape**, never colour alone: ✔ "complete", ⚠ "warning", ◔ "not scanned". The unsupported-font and not-scanned states are conveyed in words and counts. |
| Keyboard operable | **2.1.1** all functionality from keyboard | Full tab order + shortcuts in §4.4; Convert/Apply/Revert all keyboard-activatable. |
| No keyboard trap | **2.1.2** | `Esc` and `Tab` always escape menus/expanders (§4.4). |
| Visible focus | **2.4.7** | A visible focus indicator on every focusable control, ≥3:1 contrast, not removed by CSS reset. |
| Focus order | **2.4.3** logical order | Order matches reading/visual order per state (§4.4); revealed content inserted in place. |
| Focus on state change | **3.2.1 / 4.1.3** | Focus moves to the new state's primary control; `aria-live="polite"` announces the change (§4.5). |
| Headings & structure | **1.3.1** | Each state has one `<h1>`-level heading; sections use real headings; counts in a list/definition structure. |
| Labels & names | **4.1.2** name/role/value | All controls have accessible names from the bilingual strings; expanders use `aria-expanded`; toggle uses `aria-pressed`/listbox semantics; progress uses `role="progressbar"` with `aria-valuenow/min/max` (or `aria-busy` when indeterminate). |
| Live status | **4.1.3** status messages | Body is `aria-live="polite"`; errors use `role="alert"` (assertive) so they are announced immediately. |
| Language of parts | **3.1.1 / 3.1.2** | Root `lang` follows the toggle (`bn`/`en`); the inactive-language sample text and font names carry their own `lang`/`xml:lang` so screen readers pronounce Bangla and English correctly. |
| Resize / reflow | **1.4.4 resize 200%**, **1.4.10 reflow** | Layout is single-column and reflows; honours OS/Word text-size; no fixed pixel heights that clip text; the taskpane scrolls vertically, never horizontally, down to 320px-equivalent width. |
| Text spacing | **1.4.12** | No clipping when users override line-height/letter/word spacing; containers grow with content. |
| Target size | **2.5.8 (AA, 2.2)** / good practice | Primary/secondary buttons and expanders sized for comfortable hit targets (≥24×24 CSS px minimum, primary actions larger). |
| Reduced motion | **2.3.3 (AAA) honoured as policy** + **2.2.2** | Respect `prefers-reduced-motion`: progress is a static/stepped indicator, no spinners or pulsing; any transition is ≤200ms and can be disabled. No auto-advancing content; the before/after sample only advances on user action. No flashing (**2.3.1**). |
| Font sizing | scalable, not pixel-locked | Text in relative units; Bangla rendered in **Noto Sans Bengali** which carries the full conjunct/reph repertoire so previews don't show tofu; English in the system UI font. Minimum body size readable at default Word zoom. |
| Error identification | **3.3.1 / 3.3.3** | Errors state the cause in plain bilingual language and the suggested fix (e.g. "press Ctrl+Z and try again"); no jargon, no error codes as the primary message. |
| Pointer/no-hover dependence | **1.4.13** | All info revealed on hover is also reachable on focus and via click/Enter; warnings are persistent, not hover-only tooltips. |

**Out of automated reach but required:** a manual screen-reader pass (NVDA + Narrator on Windows,
VoiceOver on Mac) of the full Convert→Preview→Apply→Revert flow, and a keyboard-only pass, are
acceptance gates for the UI (tracked in the Phase 2 test plan, not here).

---

## 6. Visual / contrast token guidance

This section deliberately **does not pick final brand colours.** It specifies the *contract* the
visual layer must satisfy. Final hex values are a later step; they must be validated against these
ratios before shipping.

### 6.1 Output / preview font

- The bundled, canonical **output font is Noto Sans Bengali** (SIL OFL) — decision **D-0005**.
- The Preview "after" panel and the success confirmation **render Bangla in Noto Sans Bengali**, so
  what the user previews matches what gets written. The "before" panel renders in the document's
  original Bijoy font (so users recognise their own text). English UI uses the host/system UI font.
- The font and its `OFL.txt`/NOTICE are bundled with the add-in (referenced in About).

### 6.2 Colour token contract (specify ratios, not hex)

| Token | Used for | Required contrast |
| --- | --- | --- |
| `text/primary` on `surface/base` | body text, labels | ≥ **4.5:1** (SC 1.4.3) |
| `text/secondary` on `surface/base` | helper/caption text | ≥ **4.5:1** (treat as normal text) |
| `text/onAccent` on `accent/primary` | primary button label | ≥ **4.5:1** |
| `accent/primary` vs `surface/base` | primary button fill edge / border | ≥ **3:1** (SC 1.4.11) |
| `focus/ring` vs both its backgrounds | visible focus indicator | ≥ **3:1** on every surface it can land on |
| `status/warning` mark + its text | unsupported-font warning | icon/border ≥ 3:1; text ≥ 4.5:1; **plus a non-colour cue** (⚠ + word "warning") |
| `status/info` (not-scanned) mark + text | not-scanned report | same rule; **plus** ◔ icon + the word "not scanned" |
| `status/success` mark + text | Done confirmation | same rule; **plus** ✔ icon + the word "complete" |
| `border/control` vs `surface/base` | input/expander outlines | ≥ **3:1** |

Rules:

- **No information is carried by colour alone** (SC 1.4.1) — every status token is paired with an
  icon *and* a text label, so the UI is fully legible in greyscale and to colour-blind users.
- The token set must be validated in **both** an OS light and dark theme (Word can be either);
  ratios apply in each theme. Prefer pulling neutrals from Office UI theme variables where possible
  so Mukti matches the host chrome, but **re-verify contrast** — host themes are not guaranteed AA.
- Focus ring must remain visible on coloured (accent) buttons as well as on neutral surfaces.

---

## 7. Error & empty states (bilingual copy)

All error/empty copy is in the string table (§3). Behavioural rules:

| State | When | Copy keys | Behaviour |
| --- | --- | --- | --- |
| **Empty (E0)** | Scan finished, 0 convertible runs | `empty.heading`, `empty.body`, plus `notscanned.*` / `warn.unsupported.*` if applicable | Friendly, not an error. Offer Start over. If the reason is "all unsupported font", show the unsupported list above so the user understands *why* nothing converted. |
| **Error — scan/generic (S6)** | Engine or Office.js failure during scan/preview | `error.heading`, `error.body.generic`, `btn.retry`, `btn.startover` | Reassure that **no changes were made** (true — scan is read-only). Plain-language cause where known; never an error code as the headline. `role="alert"`. |
| **Error — apply failure (S6)** | Failure mid-write | `error.heading`, `error.body.applyfail`, `btn.retry`, `btn.startover` | Tell the user the document may be partially changed and to press **Ctrl+Z**, then retry. Honest about Word owning the undo stack. |
| **Unsupported-font (S2a)** | ≥1 Bangla-looking run on a font not in the known list | `warn.unsupported.*` | Loud, persistent, named with counts; those runs are excluded from conversion. **Never** auto-converted (H4). |
| **Not-scanned (S2b)** | ≥1 out-of-scope region present | `notscanned.*` | Always disclosed in Preview/Done by category + count. Headers/footers reported as **pending** (distinct from "not scanned"). Body + tables confirmed in-scope (M6). |

Copy principles: short sentences; the Bangla is primary and idiomatic (not a literal calque of the
English); state the *consequence* and the *next action*; never blame the user; no raw exception
text in the headline (a developer-only detail may be tucked behind an expander for bug reports, but
it must contain **no document content** — privacy promise).

---

## 8. Explicitly NOT in the MVP UI (deferred)

These are intentionally out of scope for the MVP and must **not** appear (so the UI stays small,
fast, and honest). Listing them here prevents scope creep (M1) and sets expectations.

| Deferred item | Why deferred / where it goes |
| --- | --- |
| **Selection-only conversion** | Fast-follow, not MVP (per spec). MVP is whole-document only. |
| **Headers/footers conversion** | "Pending" — surfaced in the not-scanned report as pending, not converted yet. |
| **Footnotes, text boxes, comments, fields, SmartArt conversion** | Out of scope; reported as "not scanned", never converted. |
| **Reverse conversion (Unicode → Bijoy)** | Cut (M1 / D-0003). |
| **Excel / PowerPoint UI** | Cut — Word only (D-0003). |
| **Grammar checker, proofreader, dictionary, "learning store"** | Cut (D-0003 / M1 / M4). |
| **Per-font / advanced options, font picker** | None in MVP; output font is fixed to Noto Sans Bengali (D-0005). |
| **Settings/preferences screen** | Only the language toggle persists; no general settings panel. |
| **Telemetry / analytics / feedback-upload UI** | Forbidden by the privacy promise (G1); no "send report" that transmits anything. |
| **Onboarding tour / multi-step wizard** | The single-screen state machine is the whole UX; no tour. |
| **Custom global keyboard shortcuts in Word** | MVP uses the ribbon button + in-taskpane keys only. |
| **Batch / multi-document processing** | One open document at a time. |
| **Light/dark custom theming controls** | We *follow* the host theme; no in-app theme switch. |
| **Progress cancellation during Apply** | Cancel exists for Scan (read-only) but not mid-write; Revert is the recovery path instead. |

---

## Appendix A — Cross-references

- Loud unsupported-font handling: DO-NOT-REPEAT **H4**; this doc §2.4, §7.
- Per-run reporting (no dropped mixed-font paragraphs): DO-NOT-REPEAT **H5**; this doc §4.1.
- Real preview + non-destructive revert: DO-NOT-REPEAT **H6**; this doc §2.3, §2.6, §4.2.
- "Not scanned" transparency: DO-NOT-REPEAT **M6**; this doc §2.5, §7.
- Privacy line / online-first wording: DO-NOT-REPEAT **G1**, spec; this doc §3 (`about.*`).
- Output font Noto Sans Bengali: DECISION-LOG **D-0005**; this doc §6.1.
- Word-only / scope freeze: DECISION-LOG **D-0003**; this doc §8.
- Idempotency / no-op classification of non-Bijoy runs: DECISION-LOG **D-0007**; this doc §4.1.
- Whitespace/formatting preserved: DECISION-LOG **D-0008** (preview must not imply re-spacing).
