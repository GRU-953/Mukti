# Mukti — Known limitations

Mukti converts old Bengali text (typed in Bijoy or SutonnyMJ-style fonts) into
modern Unicode Bengali inside Microsoft Word. ("Unicode" is simply the standard
way computers store Bengali today, so your text works everywhere — phones, the
web, search.)

This page lists, in plain English, what Mukti does **not** do yet, or does only
in part. We would rather tell you the honest picture up front than surprise you
later. A guiding rule runs through everything below: **Mukti is loud, never
silent.** If it cannot safely convert something, it tells you and leaves that
part untouched — it never quietly changes or mangles text behind your back.

---

## 1. What gets converted (and what gets reported instead)

**What it means for you.** Mukti converts the main body of your document and the
text inside tables. Some other parts of a Word document are not converted in this
version — but Mukti always *tells you* about them rather than skipping them
silently.

- **Headers and footers** (the repeating text at the top and bottom of pages):
  *pending*. Mukti reports that it found them but does not convert them yet.
- **Footnotes, text boxes, comments, fields** (auto-updating bits like page
  numbers or cross-references) **and SmartArt** (Word's diagram graphics): **not
  scanned**. Mukti does not look inside these at all, and it clearly reports them
  as "not scanned" so you know to check them.

**Workaround.** After converting, look at the summary Mukti gives you. For
anything listed as "pending" or "not scanned", select that text by hand and
either re-type it, or convert it in a later version once support is added. Your
body text and tables are done for you.

---

## 2. Fonts: only known Bijoy/SutonnyMJ fonts are converted

**What it means for you.** Mukti recognises a fixed list of Bijoy- and
SutonnyMJ-family fonts (the old fonts this tool was built to handle). Only text
in those recognised fonts is converted. If Mukti finds Bengali-looking text in a
font it does **not** recognise, it flags it and leaves it exactly as it was — it
never tries to convert a font it isn't sure about, because guessing would mangle
your text.

**Workaround.** If something you expected to convert was left untouched, check
which font it uses. The list of supported fonts grows over time, so a font that
isn't covered today may be added in a future version. You can also report the
font name to the maintainer so it can be considered for the list.

---

## 3. How much of the Bengali script is covered

**What it means for you.** The everyday building blocks of Bengali are fully
covered and tested: common letters, vowel signs, joined letters ("conjuncts",
where two or more consonants combine into one shape), the correct re-ordering of
characters (such as the *reph* mark and vowels that are typed after a consonant
but displayed before it), the Bengali digits, and the full stop "dari" (।).

A few rarer cases are still being worked on:

- Some uncommon pre-formed conjunct shapes are still being added.
- One specific case is a **known gap**: the *ya-phala* after র (ra) — written
  র‍্য, as in the word র‍্যাব ("RAB"). Mukti does not yet handle this correctly.

**The accuracy claim, stated honestly.** Mukti scores **at least 99% on the
project's frozen test set** — a fixed, carefully checked collection of test words
and sentences the project measures against. This is **not** a promise of "99%
accurate on any document". Your own text may contain rare cases (like the gap
above) that the test set doesn't cover. Treat 99% as "very reliable on the
material we have tested", not as a guarantee for every possible input.

**Workaround.** After converting, proofread important documents — especially any
unusual or rare conjuncts. For the र‍্य case, check those words by hand for now.

---

## 4. Undo: how to safely reverse a conversion

**What it means for you.** The reliable way to undo a conversion is the
**"Revert Mukti changes"** button. When you convert, Mukti first takes a
*snapshot* (a saved copy of the text as it was, just before converting); the
Revert button puts that snapshot back.

There is an important catch:

- The snapshot reflects the document **at the moment you converted**. If you
  **edit or save** the document afterwards, the snapshot may no longer match what
  is on the page, and reverting may not work cleanly.
- The snapshot is stored **inside the document file itself**. This means it
  travels with the file if you email or share it — so another person could, in
  principle, recover your pre-conversion text. There is a control to clear
  ("forget") this undo data when you no longer need it.

**Ctrl+Z also works** (⌘Z on a Mac), but the number of times you need to press it
to fully undo varies depending on whether you are on Windows, Mac, or Word on the
web — so it is less predictable than the Revert button.

**Workaround.** Before converting anything important, **keep a separate copy of
the original file**. Then convert, and use "Revert Mukti changes" if you need to
go back — ideally before doing further editing.

---

## 5. Mukti needs the internet to start ("online-first")

**What it means for you.** Mukti is **not** offline software. When you open it, it
downloads its program code from Microsoft and from the project's own hosting, so
you need a working internet connection at the moment it starts up. If you are
offline, it may fail to load.

**To be clear about privacy:** "online-first" refers only to *loading the tool's
code*. **Your document's content never leaves your device.** Mukti does the
conversion locally and does not send your text anywhere.

**Workaround.** Make sure you are connected to the internet when you launch
Mukti. Once it has loaded, the conversion itself happens on your own computer.

---

## 6. Not in this version (left out on purpose)

These are deliberate choices to keep the first version focused and reliable —
not bugs. Some may arrive in future versions.

- **One direction only.** Mukti converts old Bijoy/SutonnyMJ → modern Unicode. It
  does **not** convert the other way (Unicode back to Bijoy).
- **Word only.** It works in Microsoft Word, not in Excel or PowerPoint.
- **No grammar or spell-check.** Mukti only changes the text encoding; it does not
  check or correct your Bengali spelling or grammar.
- **Whole document at a time.** Mukti converts the whole document, not just a part
  you have selected. Converting only a selected portion is a planned follow-up,
  not available yet.

---

## 7. Still being confirmed in real Word

**What it means for you.** Three behaviours work as designed in the project's own
testing, but are confirmed through short hands-on tests the maintainer runs
inside real Microsoft Word. Until those tests pass, please treat these as
**expected but not yet fully confirmed**:

- **Per-word font handling** — correctly reading the font of each individual word,
  so a mix of fonts on one line is handled properly.
- **Undo fidelity** — the snapshot restoring your text and formatting exactly.
- **The text-encoding check** — confirming Word hands the text to Mukti in the
  form the converter expects (the single assumption the whole tool rests on).

**Workaround.** This mainly matters during the run-up to release. As a user, the
safe habit is the same as in section 4: keep a copy of important documents, and
proofread the result after converting.

---

*This document is kept honest on purpose: it is better to know the edges of the
tool than to be surprised by them. If you hit a limitation not listed here,
please tell the maintainer so it can be added.*
