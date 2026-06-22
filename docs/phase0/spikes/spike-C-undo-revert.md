# Spike C — undo / revert fidelity

**Question.** Can we offer a **reliable "Revert Mukti changes"** that restores the
original text *and* its formatting after a conversion? And how does Word's native
undo (Ctrl+Z) behave on our edits?

**Why it's a killer.** The prior "undo" was a destructive *reverse conversion*
(lossy, changed fonts). We promised a reliable revert and an honest story about
Ctrl+Z. This kit proves the **snapshot → restore** approach round-trips
faithfully.

**Verdict so far:** 🟡 partially de-risked (the prior versions provably landed
edits on Word's native undo stack). This kit confirms snapshot-restore fidelity.

> **Time:** ~10 minutes. This snippet **does change a selection and then restore
> it** — so use a throwaway test document, not anything important.

## How to run it

1. Install **Script Lab** if you haven't (see
   [spike-A](spike-A-per-run-font.md) step 1).
2. In a **throwaway** document, type a short line with some formatting — e.g.
   type `পরীক্ষা ১২৩`, then **bold** a couple of words and make one a different
   size/colour. Select that line.
3. Home → **Script Lab** → **Code**. Paste the snippet below into the **Script**
   box. Click **Run**.
4. Watch the document: the selection briefly changes to `(((converted)))` in a
   different font, then snaps back to your original.
5. **Read the last console line** (`RESULT: GREEN` or `RESULT: RED`) and send me
   the whole console output.

### Snippet

```js
$("#run").click(() => tryCatch(run));

async function run() {
  await Word.run(async (context) => {
    const sel = context.document.getSelection();

    // 1. SNAPSHOT the original text + formatting
    sel.load("text");
    sel.font.load("name, bold, italic, size, color");
    await context.sync();
    const snap = {
      text: sel.text, name: sel.font.name, bold: sel.font.bold,
      italic: sel.font.italic, size: sel.font.size, color: sel.font.color,
    };
    console.log("SNAPSHOT: " + JSON.stringify(snap));

    // 2. SIMULATE a conversion (replace text, change font)
    sel.insertText("(((converted)))", "Replace");
    sel.font.name = "Noto Sans Bengali";
    await context.sync();

    // 3. RESTORE from the snapshot
    const sel2 = context.document.getSelection();
    sel2.insertText(snap.text, "Replace");
    sel2.font.name = snap.name;
    sel2.font.bold = snap.bold;
    sel2.font.italic = snap.italic;
    sel2.font.size = snap.size;
    sel2.font.color = snap.color;
    await context.sync();

    // 4. VERIFY the round-trip
    sel2.load("text");
    sel2.font.load("name, bold, italic, size, color");
    await context.sync();
    const ok =
      sel2.text === snap.text &&
      sel2.font.name === snap.name &&
      sel2.font.bold === snap.bold &&
      sel2.font.italic === snap.italic &&
      sel2.font.size === snap.size &&
      sel2.font.color === snap.color;
    console.log("AFTER RESTORE: " + JSON.stringify({
      text: sel2.text, name: sel2.font.name, bold: sel2.font.bold,
      italic: sel2.font.italic, size: sel2.font.size, color: sel2.font.color,
    }));
    console.log(ok
      ? "RESULT: GREEN — snapshot/restore round-trips text + formatting exactly"
      : "RESULT: RED — something did not round-trip; send this output to the maintainer");
  });
}

async function tryCatch(callback) {
  try { await callback(); }
  catch (error) { console.error(error); }
}
```

## Bonus: the Ctrl+Z question (manual, 1 minute)

After running the snippet once, press **Ctrl+Z** a few times and count how many
presses it takes to get back to your original line. Tell me the number. (We
expect it to vary by platform — that's fine; it's why we provide a dedicated
"Revert Mukti changes" command rather than relying on Ctrl+Z.)

## How to read the result

- **GREEN:** the snapshot→restore approach faithfully restores text and
  formatting → we can build a reliable "Revert Mukti changes" command. 
- **RED:** send the output; we adjust which properties we snapshot before
  building.

## Rollback

Use a throwaway document. If anything looks off, just close the document without
saving, or press Ctrl+Z until the original returns.
