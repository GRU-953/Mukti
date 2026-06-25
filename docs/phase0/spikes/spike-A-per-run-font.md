# Spike A — per-run font access in Word

**Question.** Inside one paragraph that mixes fonts (e.g. some SutonnyMJ Bangla
and some English in Calibri), can Mukti read the font of **each part** so it
converts the Bijoy part and leaves the English alone — instead of silently
skipping the whole paragraph (the prior engine's bug)?

**Verdict so far:** 🟡 partially de-risked (the prior versions provably read a
paragraph's font in production). This kit confirms the finer **per-word**
granularity. Run it once in Word and send me the result.

> **Time:** ~15 minutes. **You cannot break anything** — this reads fonts and
> prints them; it does not change your document.

## How to run it (step by step)

1. **Install Script Lab** (a free Microsoft tool that runs little test snippets
   inside Word):
   - In Word, go to the **Insert** tab → **Get Add-ins** (or **Add-ins** →
     **Get Add-ins**).
   - Search for **Script Lab**, click **Add**. A "Script Lab" button appears on
     the **Home** tab.
2. **Make a test paragraph.** In a blank document, type one line that mixes
   fonts, for example:
   - Type some Bangla in a Bijoy font: set the font to **SutonnyMJ** and type
     `Avwg` (this shows as আমি-looking Bijoy text), then a space,
   - then switch the font to **Calibri** and type `Hello`,
   - then a space and switch to **Times New Roman** and type `2025`.
   - Now select that whole line (click at the start, shift-click at the end).
3. **Open Script Lab:** Home tab → **Script Lab** → **Code**. Delete whatever is
   in the **Script** box and paste the snippet below. (Leave HTML/CSS empty.)
4. Click **Run** (top of Script Lab). A console panel appears at the bottom.
5. **Read the last line.** It says either `RESULT: GREEN ...` or `RESULT: RED ...`.
6. **Copy the whole console output** and paste it back to me. That's it.

### Snippet (paste into the Script Lab "Script" box)

```js
Office.onReady(() => run().catch((e) => console.error(e)));

// Self-contained: appends a 3-word line, sets a DIFFERENT font on each word,
// then reads the fonts back per word. No manual selecting needed — just open a
// BLANK / throwaway document and click Run (it only appends a test line).
async function run() {
  await Word.run(async (context) => {
    const has13 = Office.context.requirements.isSetSupported("WordApi", "1.3");
    console.log("WordApi 1.3 supported: " + has13);

    const p = context.document.body.insertParagraph("Avwg Hello 2025", "End");
    await context.sync();

    const words = p.getTextRanges([" "], true); // split on spaces -> per word
    words.load("items");
    await context.sync();

    const fonts = ["SutonnyMJ", "Calibri", "Times New Roman"];
    words.items.forEach((r, i) => { r.font.name = fonts[i % fonts.length]; });
    await context.sync();

    words.items.forEach((r) => { r.load("text"); r.font.load("name"); });
    await context.sync();

    const distinct = new Set();
    words.items.forEach((r) => {
      console.log("word: " + JSON.stringify(r.text) + "  font: " + JSON.stringify(r.font.name));
      if (r.font.name) distinct.add(r.font.name);
    });
    console.log("words found: " + words.items.length +
      "; distinct fonts read back: " + distinct.size + " [" + [...distinct].join(", ") + "]");
    console.log(has13 && words.items.length >= 2 && distinct.size >= 2
      ? "RESULT: GREEN — per-word font reading works (each word's font read independently)"
      : "RESULT: RED or PARTIAL — send this whole output to the maintainer");
  });
}
```

## How to read the result

- **GREEN** (2+ distinct fonts read, WordApi 1.3 supported): we can detect which
  parts of a paragraph are Bijoy vs English at word level → no more silently
  skipped mixed paragraphs. The engine design proceeds as planned.
- **RED / PARTIAL** (fonts come back blank/`null`, or only one font): send me the
  output; we adapt the detection strategy (e.g. finer-grained ranges) before
  building. This is exactly the kind of result that *reshapes the plan*, which is
  what the spike is for.

## Rollback

Nothing to roll back — the snippet only reads. You can delete the test paragraph
and uninstall Script Lab anytime (Home → Script Lab does nothing to your real
documents).
