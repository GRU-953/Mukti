# Spike D — the encoding seam (the existential check)

**Question.** When a real SutonnyMJ/Bijoy document sits in Word, does Office.js
`Range.text` hand us **exactly the code points our corpus assumes** (the
Windows‑1252 reading of the font's bytes, e.g. the e‑kar glyph as `‡` = U+2021)?
Everything — the corpus, the engine, the 100% spike — rests on this. It has never
been tested through Word; both our oracle and our converter share the same
assumption, so their agreement does **not** prove it (Phase 3 red‑team R1).

**Verdict so far:** 🔴 **untested — highest‑priority gate.** If Word returns the
raw C1 bytes (U+0080–U+009F) instead of the Windows‑1252 characters, the e‑kar
that drives every pre‑base reorder is missed and conversion silently mangles.

> **Time:** ~15 minutes. **Reads only — changes nothing.** You need one real
> SutonnyMJ document (or type a few words in the SutonnyMJ font).

## How to run it

1. Install **Script Lab** (see [spike‑A](spike-A-per-run-font.md) step 1).
2. Open a document containing **SutonnyMJ/Bijoy** text. If you don't have one:
   set the font to **SutonnyMJ** and type `Avwg evsjv‡`k` (it should look like
   আমি‑style Bijoy). **Select that text.**
3. Home → **Script Lab** → **Code** → paste the snippet below into **Script** →
   **Run**.
4. Read the console. For the selected text it prints each character and its code
   point, and a line `RESULT: ...`.
5. **Copy the whole console output to me.** I compare the printed code points
   against the corpus `source` arrays for the same words.

### Snippet

```js
$("#run").click(() => tryCatch(run));

async function run() {
  await Word.run(async (context) => {
    const sel = context.document.getSelection();
    sel.load("text");
    await context.sync();

    const text = sel.text || "";
    const points = Array.from(text).map(
      (ch) => "U+" + ch.codePointAt(0).toString(16).toUpperCase().padStart(4, "0")
    );
    console.log("TEXT: " + JSON.stringify(text));
    console.log("CODE POINTS: " + points.join(" "));

    // Heuristic check: SutonnyMJ Bijoy text should contain Windows-1252
    // punctuation glyphs (e.g. U+2018/2019/201C/201D/2020/2021/0152/0160),
    // NOT raw C1 control bytes (U+0080..U+009F).
    const hasC1 = Array.from(text).some((c) => {
      const n = c.codePointAt(0);
      return n >= 0x80 && n <= 0x9f;
    });
    const looksWin1252 = /[‘’“”†‡ŒœŠˆ©®]/.test(text);
    console.log("contains raw C1 control bytes (BAD): " + hasC1);
    console.log("contains Windows-1252 glyphs (expected for Bijoy): " + looksWin1252);
    console.log(
      hasC1
        ? "RESULT: RED — Word returned raw C1 bytes; corpus/engine assumption WRONG, send this to the maintainer"
        : "RESULT: LIKELY OK — send the CODE POINTS line so the maintainer can confirm against the corpus"
    );
  });
}

async function tryCatch(callback) {
  try { await callback(); }
  catch (error) { console.error(error); }
}
```

## How to read it

- **CODE POINTS match the corpus `source` arrays** (e.g. `‡` shows as `U+2021`):
  🟢 the foundation is sound; Phase 4 build proceeds.
- **"contains raw C1 control bytes: true"** or points that don't match: 🔴 send me
  the output — the engine's input layer needs a byte‑decoding step, and the
  corpus `source` arrays may need regenerating. This would reshape the plan
  *before* any code is written — exactly the spike's job.

## Rollback
None needed — it only reads the selection.
