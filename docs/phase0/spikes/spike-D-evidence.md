# Spike D — encoding-seam evidence (web research)

> ## ✅ CONFIRMED IN WORD (2026-06-25)
> Run via Script Lab in **Word on the web**. Selecting `Avwg evsjv‡` (SutonnyMJ)
> and reading `Range.text` returned code points
> `U+0041 U+0076 U+0077 U+0067 U+0020 U+0065 U+0076 U+0073 U+006A U+0076 U+2021` —
> the e-kar `‡` came through as **U+2021**, ASCII as ASCII, **no C1 bytes**.
> These match the corpus `source` arrays byte-for-byte (`word-ami`, `word-bangla`,
> `kar-e`), and the engine converts the sample to `আমি বাংলা`. The high-confidence
> research below is now **empirically confirmed**; the assumption holds. (Confirmed
> on Word-on-web; desktop is expected identical, as this is a document-storage
> property, not host rendering — a desktop re-run is a nice-to-have, not a gate.)


**Question.** When a Word document holds text typed in a legacy 8-bit Bengali
font (SutonnyMJ/Bijoy) and an Office.js add-in reads `Range.text`, does it hand
us the **Windows-1252 / CP1252 interpretation** of the font's bytes — i.e. the
exact code points a Bijoy→Unicode converter expects (e-kar `‡` = U+2021,
copyright `©` = U+00A9, grave `` ` `` = U+0060) — or could Word surface raw C1
control bytes (U+0080–U+009F) or something else?

> Scope note: this is desk research from authoritative sources. It does **not**
> replace the ~15-minute in-Word Script Lab confirmation in
> [spike-D-encoding.md](spike-D-encoding.md) — it tells us what to *expect* and
> how confident to be while building.

## Answer

**Yes — `Range.text` returns the CP1252 code points our corpus assumes**, with
high confidence. The reasoning chain is short and each link is sourced:

1. **A `.docx` stores run text as Unicode characters, not bytes.** The body is
   an XML 1.0 / UTF-8 file (`word/document.xml`) and a text run's content lives
   as XML character data inside `<w:t>`. There is no "byte" layer left at this
   point — the characters are already Unicode code points.
2. **Legacy 8-bit "ANSI" font text is decoded through the Windows ANSI code
   page = CP1252.** SutonnyMJ is an **ANSI** TrueType font (its bytes are typed
   in "ANSI mode"), *not* a symbol-charset font. So each byte 0x80–0xFF was
   interpreted as its CP1252 character when the text became Unicode (the moment
   it was typed/stored/round-tripped), e.g. 0x87 → U+2021 `‡`, 0x88 → U+02C6
   `ˆ`, 0xA9 → U+00A9 `©`, 0x91/0x92/0x93 → U+2018/2019/201C. The
   `U+F020–U+F0FF` Private-Use remapping that *would* break this applies **only
   to symbol-charset fonts** (charset 2, e.g. Wingdings) — not to an ANSI text
   font like SutonnyMJ.
3. **Office.js `Range.text` returns a normal JavaScript string**, which is
   UTF-16 of those stored Unicode characters. The proxy is `load()`-ed then
   read after `context.sync()`; the value is "the text of the range" verbatim —
   no re-encoding step is documented or expected.
4. **Real Bijoy→Unicode converters key their maps on exactly these CP1252 code
   points.** Three independent converter source trees use map keys
   `† ‡ ˆ ‰ Š ™ š › • —` (the CP1252-only 0x80–0x9F glyphs) and the curly quotes
   `‘ ’ “` — precisely the code points produced by step 2. This is decisive:
   these glyphs do *not* exist in ISO-8859-1/Latin-1 (whose 0x80–0x9F are C1
   controls), so the entire ecosystem is built on the CP1252 reading, the same
   one our corpus assumes.

## Strongest evidence (quote + URL)

1. **`.docx` text is Unicode XML character data (UTF-8 `document.xml`).**
   > "The format uses XML 1.0 with UTF-8 encoding … The `w:t` element (where 't'
   > stands for text) is used to contain character data in WordprocessingML
   > documents."
   — ECMA-376 / OOXML overview, via Ecma International.
   <https://ecma-international.org/publications-and-standards/standards/ecma-376/>
   (structure confirmed at <https://edutechwiki.unige.ch/en/Open_Packaging_Conventions_and_Office_Open_XML>)

2. **The F020–F0FF remap is symbol-charset-only; ANSI fonts keep native
   0x20–0xFF values.** (This is the failure mode that does *not* apply to us.)
   > "in a symbol-charset font like Wingdings, it stands for whatever character
   > has hex code 0041 … so the Word 97 folks decided to give it a distinct
   > value, namely F000 + 41 = F041 … A key point here is that Word RTF may
   > treat any **symbol-charset** character this way … The charset is specified
   > by the `\fcharset`N control word and the symbol-charset is N = 2."
   — Murray Sargent (Microsoft), "Weird F020-F0FF characters in Word's RTF".
   <https://learn.microsoft.com/en-us/archive/blogs/murrays/weird-f020-f0ff-characters-in-words-rtf>

3. **SutonnyMJ is an ANSI font, output in "ANSI mode" — not a symbol font.**
   > "SutonnyMJ font is an ANSI Bangla font … set Avro keyboard in ANSI mode by
   > clicking … 'Output as ANSI'."
   — kivabe.com / bengalifonts.net (corroborated across multiple font sources).
   <https://kivabe.com/en/write-sutonnymj-font-avro-keyboard/>

4. **Converters key on the CP1252 code points (the clincher).** Verified
   verbatim across three independent repos. Representative keys:
   ```php
   // bahar/BijoyToUnicode  (bijoy2unicode.php)
   '†' => 'ে', '‡' => 'ে', 'ˆ' => 'ৈ', 'Š' => 'ৗ', '©' => 'র্', '®' => 'ষ্',
   ```
   ```python
   # rabiulislam-xyz/bijoy-to-unicode-converter-python  (bijoy_to_unicode.py)
   '‘': '্থ', '’': '্তু', '“': 'চ্', '—': '্ত', '©': 'র্',
   ```
   `† ‡ ˆ ‰ Š ™ š › • —` are the CP1252 glyphs for bytes 0x86 0x87 0x88 0x89
   0x8A 0x99 0x9A 0x9B 0x95 0x97 — they have **no** Latin-1 equivalent, so the
   maps can only have been built against a CP1252 decode.
   <https://raw.githubusercontent.com/bahar/BijoyToUnicode/master/bijoy2unicode.php>
   <https://raw.githubusercontent.com/rabiulislam-xyz/bijoy-to-unicode-converter-python/master/bijoy_to_unicode.py>
   <https://github.com/rupaai/bijoy-to-unicode-converter>

5. **`Range.text` is a plain string of "the text of the range."**
   > "`readonly text: string;` … Gets the text of the range." (no encoding
   > caveat; a JS string is UTF-16 of the stored Unicode characters.)
   — Word JavaScript API, `Word.Range`, Microsoft Learn.
   <https://learn.microsoft.com/en-us/javascript/api/word/word.range>

6. **Official CP1252→Unicode table** (so the specific code points are exact):
   0x87→U+2021, 0x88→U+02C6, 0x91–0x94→U+2018/2019/201C/201D, 0xA9→U+00A9.
   <https://www.unicode.org/Public/MAPPINGS/VENDORS/MICSFT/WINDOWS/CP1252.TXT>

## Residual uncertainty

- **No Microsoft doc states `Range.text`'s encoding explicitly.** The string
  type is documented; "UTF-16 of the stored characters" is the unavoidable
  reading (JS strings are UTF-16; OOXML stores Unicode), but it is inference,
  not a quoted Microsoft sentence. *Low* risk.
- **The C1-byte failure mode is theoretically possible** if some Word build (or
  Word-on-the-web) ever surfaced bytes 0x80–0x9F as raw C1 controls. We found
  **no evidence** of this; everything points to a single Unicode-character model
  and the ecosystem's CP1252 maps would not work if it happened. Still, it is
  the one thing only a live host can disprove — hence the Script Lab kit's
  explicit `hasC1` red-flag check.
- **The surveyed converters consume strings, not `.docx` directly** (none calls
  python-docx / `paragraph.text`); they assume the caller already handed them a
  CP1252-decoded Unicode string. This *supports* our model (the CP1252
  assumption is baked into the map keys) but does not independently prove that
  Word→Office.js performs that decode. Our chain (steps 1–3) covers that gap;
  the in-Word run closes it.
- **Per-glyph corpus coverage is a separate axis.** This spike confirms the
  *encoding* (which code points arrive); it does not re-verify that every
  SutonnyMJ glyph our corpus uses is mapped — that is Spike B / corpus scope.

## Confidence rating

**HIGH** that `Range.text` returns the CP1252 code points our corpus assumes.
The chain is independently corroborated at every link (OOXML stores Unicode →
ANSI font decoded as CP1252, not the symbol-charset PUA path → JS string is
UTF-16 → the whole converter ecosystem keys on those exact CP1252 code points).
The only thing desk research cannot do is observe the live host, so it is not
"Certain".

## Verdict

**Proceed with the build on the CP1252 assumption** — the evidence is strong and
mutually reinforcing. Keep the assumption gated behind the ~15-minute in-Word
Script Lab run ([spike-D-encoding.md](spike-D-encoding.md)); a `hasC1: true`
result there is the one outcome that would force a byte-decoding input layer, so
do not ship before that GREEN/RED line is captured on both desktop and
Word-on-the-web.
