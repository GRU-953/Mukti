# SutonnyMJ / Bijoy → Unicode Bengali: Cited Reference

Research date: 2026-06-22. All mappings below are either quoted from cited sources or
**verified by running an actual open-source converter** (rabiulislam-xyz, downloaded and
executed locally during this research). Anything not verified is flagged in
"CONFIDENCE & CAVEATS".

---

## PRIMARY SOURCES (most reliable first)

1. **rabiulislam-xyz/bijoy-to-unicode-converter-python** — pure-Python SutonnyMJ converter,
   well-commented, includes the reordering passes (reph, pre-base kar, ya/ra-phala).
   Mapping file `bijoy_to_unicode.py`. This is the source I executed to verify every
   worked example below.
   - Repo: https://github.com/rabiulislam-xyz/bijoy-to-unicode-converter-python
   - Raw map: https://raw.githubusercontent.com/rabiulislam-xyz/bijoy-to-unicode-converter-python/master/bijoy_to_unicode.py
   - README states the glyph map is "tuned for SutonnyMJ, the most common Bijoy font."
2. **bahar/BijoyToUnicode** — established PHP converter (`bijoy2unicode.php`); the
   rabiulislam Python port is derived from this lineage of community converters.
   - https://github.com/bahar/BijoyToUnicode
3. **Microsoft — "Developing OpenType Fonts for Bengali Script"** — authoritative on the
   reorder rules (reph, pre-base matra, ya-phala/ra-phala, logical vs display order).
   - https://learn.microsoft.com/en-us/typography/script-development/bengali
4. **The Unicode Standard, Core Spec, Chapter 12 (South and Central Asia-I)** — phonetic
   storage order and reph rule (Devanagari rules R2/R15 apply to all Indic scripts incl. Bengali).
   - https://www.unicode.org/versions/Unicode16.0.0/core-spec/chapter-12/
5. **Unicode Bengali FAQ** — ya-phala vs reph and ZWJ usage.
   - http://unicode.org/faq/bengali.html
6. **Wikipedia / Wiktionary — Bengali (Unicode block)** — full U+0980–U+09FF chart.
   - https://en.wikipedia.org/wiki/Bengali_(Unicode_block)
   - https://en.wiktionary.org/wiki/Appendix:Unicode/Bengali

---

## (a) KEY → GLYPH MAPPING ROWS

Source: `bijoy_to_unicode.py` (rabiulislam-xyz), quoted verbatim from the `conversionMap`
dictionary. URL: https://raw.githubusercontent.com/rabiulislam-xyz/bijoy-to-unicode-converter-python/master/bijoy_to_unicode.py

### Independent vowels (swarborno)
| Key | Glyph | | Key | Glyph |
|-----|-------|-|-----|-------|
| `Av` | আ | | `G` | এ |
| `A`  | অ | | `H` | ঐ |
| `B`  | ই | | `I` | ও |
| `C`  | ঈ | | `J` | ঔ |
| `D`  | উ | | | |
| `E`  | ঊ | | | |
| `F`  | ঋ | | | |

### Consonants (banjonborno)
| Key | Glyph | Key | Glyph | Key | Glyph | Key | Glyph |
|-----|-------|-----|-------|-----|-------|-----|-------|
| `K` | ক | `U` | ট | `c` | প | `k` | শ |
| `L` | খ | `V` | ঠ | `d` | ফ | `l` | ষ |
| `M` | গ | `W` | ড | `e` | ব | `m` | স |
| `N` | ঘ | `X` | ঢ | `f` | ভ | `n` | হ |
| `O` | ঙ | `Y` | ণ | `g` | ম | `o` | ড় |
| `P` | চ | `Z` | ত | `h` | য | `p` | ঢ় |
| `Q` | ছ | `_` | থ | `i` | র | `q` | য় |
| `R` | জ | `` ` `` | দ | `j` | ল | | |
| `S` | ঝ | `a` | ধ | | | | |
| `T` | ঞ | `b` | ন | | | | |

### Vowel signs (kar) — note: `w`, `†/‡` are PRE-BASE (drawn left, stored after consonant)
| Key | Glyph | Meaning |
|-----|-------|---------|
| `v` | া | aa-kar (post-base) |
| `w` | ি | i-kar (PRE-base) |
| `x` | ী | ii-kar (post-base) |
| `y` / `z` | ু | u-kar (post-base) |
| `~` | ূ | uu-kar (post-base) |
| `„` / `…` | ৃ | ri-kar (post-base) |
| `†` / `‡` | ে | e-kar (PRE-base) |
| `ˆ` / `‰` | ৈ | ai-kar (PRE-base) |
| `Š` | ৗ | au-length-mark (combines with e-kar → ৌ) |

o-kar ো and au-kar ৌ are **not single keys**: ো = `‡`+consonant+`v` (e→o reorder),
ৌ = `‡`+consonant+`Š`. Confirmed by running the converter (`n‡jv` → হলো).

### Special signs
| Key | Glyph | Name | Code point |
|-----|-------|------|-----------|
| `s` | ং | anusvara | U+0982 |
| `t` | ঃ | visarga | U+0983 |
| `u` | ঁ | candrabindu | U+0981 |
| `r` | ৎ | khanda-ta | U+09CE |
| `\&` | ্‌ | hosonto/virama (explicit) | U+09CD |
| `\|` | । | danda (full stop) | U+0964 |

### Bengali digits
| `0`→০ | `1`→১ | `2`→২ | `3`→৩ | `4`→৪ | `5`→৫ | `6`→৬ | `7`→৭ | `8`→৮ | `9`→৯ |

### Conjuncts / hosonto linking
In SutonnyMJ, conjuncts are **pre-formed single glyph slots**, not built by typing a
hosonto between two consonants. The converter maps each ligature glyph directly to its
Unicode C+্+C sequence. Verbatim examples from `conversionMap`:
```
'¨': '্য'      (ya-phala)
'©': 'র্'      (reph: ra + halant, appended AFTER its cluster in Bijoy, then reordered)
'ª' / '«' / 'Ö': '্র'   (ra-phala)
'°': 'ক্ক'   '¹'/'Á': 'জ্ঞ'   'Æ': 'ট্ট'   'Ë': 'ত্ত'   'Î': 'ত্র'
'¶'/'ÿ': 'ক্ষ'   '³': 'ক্ত'   'µ': 'ক্র'   '½': 'ঙ্গ'   '×': 'দ্ধ'
'\^': '্ব'  '£': '্ভ্র'  '²': 'ক্ষ্ণ'
```
The hosonto in `র্` (reph) and `্য`/`্র` (phalas) is **stored in C+্+C order in Unicode**;
the converter's reordering passes (`_move_reph`, `_swap_ra_halant_kar`, `_move_pre_kars`)
move the Bijoy display-order glyphs into correct Unicode logical order.

---

## (b) WORKED WORD EXAMPLES (ASCII Bijoy → Unicode)

All rows below were produced by **running `convertBijoyToUnicode()` from the rabiulislam
converter** during this research (not hand-typed), then NFC-normalized and code-point-dumped.
Converter source: https://github.com/rabiulislam-xyz/bijoy-to-unicode-converter-python

| Bijoy ASCII | Unicode output | NFC code points | Notes |
|-------------|----------------|-----------------|-------|
| `Avwg` | আমি | U+0986 U+09AE U+09BF | classic example |
| `evsjv` | বাংলা | U+09AC U+09BE U+0982 U+09B2 U+09BE | anusvara ং |
| `evsjv‡`k` | বাংলাদেশ | U+09AC U+09BE U+0982 U+09B2 U+09BE U+09A6 U+09C7 U+09B6 | e-kar reorder |
| `K` | ক | U+0995 | single consonant |
| `Kvj` | কাল | U+0995 U+09BE U+09B2 | aa-kar |
| `wK` | কি | U+0995 U+09BF | **pre-base i-kar reordered after ক** |
| `‡`k` | দেশ | U+09A6 U+09C7 U+09B6 | **pre-base e-kar reordered after দ** |
| `eB` | বই | U+09AC U+0987 | |
| `Avi` | আর | U+0986 U+09B0 | |
| `ag©` | ধর্ম | U+09A7 U+09B0 U+09CD U+09AE | **reph: র+্ stored before ম** |
| `Kg©` | কর্ম | U+0995 U+09B0 U+09CD U+09AE | **reph** |
| `cÖ` | প্র | U+09AA U+09CD U+09B0 | **ra-phala প+্+র** |
| `we`¨v` | বিদ্যা | U+09AC U+09BF U+09A6 U+09CD U+09AF U+09BE | **ya-phala দ+্+য** + i-kar + aa-kar |
| `n‡jv` | হলো | U+09B9 U+09B2 U+09CB | e-kar + aa-kar → o-kar U+09CB |
| `fvlv` | ভাষা | U+09AD U+09BE U+09B7 U+09BE | |
| `1995` | ১৯৯৫ | U+09E7 U+09EF U+09EF U+09EB | digits |
| `ÒAvwgÓ` | "আমি" | quote glyphs Ò/Ó → ASCII quotes | curly-quote handling |

Every output above passed `NFC == raw` (already in NFC) for the pure-letter words.

Additional example quoted from the converter README (input→output):
```
cvwievwiK Drm‡e Zvwjeyj Bj‡gi AskMÖnY Kvg¨ n‡j
  → পারিবারিক উৎসবে তালিবুল ইলমের অংশগ্রহণ কাম্য হলে
```
Source: https://github.com/rabiulislam-xyz/bijoy-to-unicode-converter-python (README)

And widely cited in converter docs:
```
Avwg evsjv fvwl → আমি বাংলা ভাষি   (per bangladatetoday guide)
```
Source: https://www.bangladatetoday.com/blog/bijoy-to-unicode-avro-guide

---

## (c) UNICODE STORAGE-ORDER RULES (with sources)

### Phonetic / logical storage order (pre-base vowels stored AFTER consonant)
> "The storage of plain text in Devanagari and all other Indic scripts generally follows
> phonetic order; that is, a CV syllable with a dependent vowel is always encoded as a
> consonant letter C followed by a vowel sign V in the memory representation."
>
> "Because Devanagari and other Indic scripts have some dependent vowels that must be
> depicted to the left side of their consonant letter, the software that renders the Indic
> scripts must be able to reorder elements in mapping from the logical (character) store to
> the presentational (glyph) rendering."

— The Unicode Standard, Core Spec, Chapter 12.
https://www.unicode.org/versions/Unicode16.0.0/core-spec/chapter-12/

Microsoft OpenType Bengali doc corroborates (pre-base matras i/e/ai = `09BF, 09C7, 09C8`
have reorder class **BeforeHalf**), i.e. displayed left, stored after:
> "In a text sequence, these characters are stored in phonetic order (although they may not
> be represented in phonetic order when displayed)."
>
> Character reordering classes for Bengali: `09BF, 09C7, 09C8 → BeforeHalf`.

— https://learn.microsoft.com/en-us/typography/script-development/bengali

**Practical rule:** ি (U+09BF), ে (U+09C7), ৈ (U+09C8) render to the LEFT of the
consonant but are STORED immediately AFTER it. Verified: `wK`→কি = `U+0995 U+09BF`.

### Reph (র্ = ra + hosonto before a consonant)
> "If the dead consonant RAd precedes a consonant, then it is replaced by the superscript
> nonspacing mark RAsup, which is positioned so that it applies to the logically subsequent
> element in the memory representation."

— Unicode Core Spec Ch.12 (Devanagari rule R2; applies to Bengali reph).
https://www.unicode.org/versions/Unicode16.0.0/core-spec/chapter-12/

Microsoft: "Reph — the above-base form of the letter 'Ra' that is used in Bengali when 'Ra'
is the first consonant in the syllable and is not the base consonant." Reorder class
`09B0, 09F0 (reph) → AfterSubscript`.
— https://learn.microsoft.com/en-us/typography/script-development/bengali

**Practical rule:** স্টোর'র় the reph is stored as **র U+09B0 + ্ U+09CD + (following
consonant)** at the START of the cluster, even though it draws as a hook above the
following consonant. Verified: `ag©`→ধর্ম = `U+09A7 U+09B0 U+09CD U+09AE` (ধ, then র্,
then ম).

### Ya-phala (্য) and Ra-phala (্র)
Both are stored as **(base consonant) + ্ (U+09CD virama) + য / র**, i.e. virama precedes
the ya/ra, which renders below/after the base.

> "When U+09AF BENGALI LETTER YA occurs as the last member of a consonant cluster it has a
> special shape called ya-phalā ... just type the underlying sequence of characters as you
> would for any other consonant cluster."
>
> "On the rare occasions when you want to retain the ya-phalā shape when ya follows ra,
> e.g. র‍্য, add U+200D ZERO WIDTH JOINER before the hasant." (otherwise ra+hasant+ya
> forms reph, not ya-phala)

— Unicode Bengali FAQ. http://unicode.org/faq/bengali.html

Microsoft confirms ra-phala is a below-base form: "Apply feature 'blwf' to substitute
below-base forms (ba + ra phala)."
— https://learn.microsoft.com/en-us/typography/script-development/bengali

**Verified:** `we`¨v`→বিদ্যা = `...U+09A6 U+09CD U+09AF...` (দ+্+য); `cÖ`→প্র =
`U+09AA U+09CD U+09B0` (প+্+র).

---

## (d) UNICODE CODE POINTS — Bengali block U+0980–U+09FF

Source: Wikipedia "Bengali (Unicode block)" + Wiktionary Appendix:Unicode/Bengali
(cross-checked against Python `unicodedata`). 96 of 128 positions assigned.
- https://en.wikipedia.org/wiki/Bengali_(Unicode_block)
- https://en.wiktionary.org/wiki/Appendix:Unicode/Bengali

### Signs / marks
| CP | Char | Name |
|----|------|------|
| U+0980 | ঀ | BENGALI ANJI |
| U+0981 | ঁ | BENGALI SIGN CANDRABINDU |
| U+0982 | ং | BENGALI SIGN ANUSVARA |
| U+0983 | ঃ | BENGALI SIGN VISARGA |
| U+09BC | ় | BENGALI SIGN NUKTA |
| U+09CD | ্ | BENGALI SIGN VIRAMA (hosonto) |
| U+09CE | ৎ | BENGALI LETTER KHANDA TA |
| U+09D7 | ৗ | BENGALI AU LENGTH MARK |

### Independent vowels
| U+0985 অ A | U+0986 আ AA | U+0987 ই I | U+0988 ঈ II | U+0989 উ U | U+098A ঊ UU |
| U+098B ঋ VOCALIC R | U+098C ঌ VOCALIC L | U+098F এ E | U+0990 ঐ AI | U+0993 ও O | U+0994 ঔ AU |

### Consonants
| U+0995 ক KA | U+0996 খ KHA | U+0997 গ GA | U+0998 ঘ GHA | U+0999 ঙ NGA |
| U+099A চ CA | U+099B ছ CHA | U+099C জ JA | U+099D ঝ JHA | U+099E ঞ NYA |
| U+099F ট TTA | U+09A0 ঠ TTHA | U+09A1 ড DDA | U+09A2 ঢ DDHA | U+09A3 ণ NNA |
| U+09A4 ত TA | U+09A5 থ THA | U+09A6 দ DA | U+09A7 ধ DHA | U+09A8 ন NA |
| U+09AA প PA | U+09AB ফ PHA | U+09AC ব BA | U+09AD ভ BHA | U+09AE ম MA |
| U+09AF য YA | U+09B0 র RA | U+09B2 ল LA | U+09B6 শ SHA | U+09B7 ষ SSA |
| U+09B8 স SA | U+09B9 হ HA | | | |

### Additional (precomposed) consonants with nukta
| U+09DC ড় RRA | U+09DD ঢ় RHA | U+09DF য় YYA |
NOTE: see caveat below — these have canonical decompositions.

### Dependent vowel signs (kar)
| U+09BE া AA | U+09BF ি I | U+09C0 ী II | U+09C1 ু U | U+09C2 ূ UU |
| U+09C3 ৃ VOCALIC R | U+09C4 ৄ VOCALIC RR | U+09C7 ে E | U+09C8 ৈ AI |
| U+09CB ো O | U+09CC ৌ AU | U+09E2 ৢ VOCALIC L | U+09E3 ৣ VOCALIC LL |

### Digits
| U+09E6 ০ | U+09E7 ১ | U+09E8 ২ | U+09E9 ৩ | U+09EA ৪ | U+09EB ৫ | U+09EC ৬ | U+09ED ৭ | U+09EE ৮ | U+09EF ৯ |

### Currency / misc
| U+09F3 ৳ BENGALI RUPEE SIGN | U+09FA ৺ ISSHAR | U+09FE ৾ SANDHI MARK |

---

## (e) CONFIDENCE & CAVEATS

1. **HIGH confidence — letter/digit/sign mapping and worked examples.** The `conversionMap`
   rows and all worked word examples were obtained by reading AND executing the
   rabiulislam-xyz converter. The classic examples (Avwg→আমি, evsjv→বাংলা) match
   widely-published references. Code points were dumped with Python `unicodedata`.

2. **CRITICAL NFC caveat — ড়/ঢ়/য় normalize to DECOMPOSED form.** The precomposed atomic
   characters ড় U+09DC, ঢ় U+09DD, য় U+09DF are **Composition Exclusions** in Unicode.
   Therefore **NFC normalizes them to base + nukta**:
   - ড় → `U+09A1 U+09BC` (DDA + NUKTA)
   - ঢ় → `U+09A2 U+09BC` (DDHA + NUKTA)
   - য় → `U+09AF U+09BC` (YA + NUKTA)
   Verified with `unicodedata.normalize("NFC", ...)`. The rabiulislam converter already
   emits the decomposed (NFC) forms. **For a gold-standard NFC corpus, expected output for
   these letters MUST use base+U+09BC, not the atomic U+09DC/U+09DD/U+09DF.** If your test
   data uses the atomic forms, run NFC first or the comparison will fail.

3. **MEDIUM confidence — conjunct glyph slots.** SutonnyMJ has hundreds of pre-formed
   conjunct glyph slots (the high-byte `°±²…þ` range). I quoted a representative subset
   from `conversionMap`; the full set is large and individual rare ligatures should be
   spot-checked against the converter. The code itself contains in-line comments flagging
   tricky glyphs (e.g. `Í`, `æ`, `Í`=ন্ত/স্ত context-dependent) — these are encoding
   quirks where a glyph's correct output depends on the preceding halant-providing glyph.

4. **o-kar / au-kar are composed, not single keys.** ো (U+09CB) and ৌ (U+09CC) are formed
   by e-kar (`‡`) wrapping the consonant plus a trailing া/ৗ; the converter's
   `_move_pre_kars` pass combines ে+া→ো and ে+ৗ→ৌ. Verified (n‡jv→হলো).

5. **Reph reorder is converter logic, not a fixed key.** In Bijoy, the reph glyph `©` is
   typed/stored AFTER its base cluster and the converter moves র্ to the front
   (`_move_reph`). The Unicode storage rule (র+্+C at cluster start) is confirmed by the
   Unicode Core Spec and verified in output. The Bijoy *input* ordering is the opposite of
   the Unicode *output* ordering — important when round-tripping.

6. **Unicode Core Spec quotes** are from the Devanagari section (rules R2, R15) which the
   spec explicitly states apply to "all other Indic scripts" including Bengali. The Bengali
   FAQ confirms ya-phala/reph + ZWJ behavior directly. The Microsoft OpenType doc is the
   most Bengali-specific source for reorder classes.

7. **UNVERIFIED:** I did not independently fetch the bahar `bijoy2unicode.php` mapping array
   contents (the GitHub raw path returned 404 / API empty); I relied on the rabiulislam
   port which the README states descends from that lineage. The two are reported as
   consistent but I could not line-by-line diff them. Also, "Avwg evsjv fvwl → আমি বাংলা
   ভাষি" from the bangladatetoday blog is reproduced as cited but note `fvwl` would more
   typically be ভাষি; treat third-party blog examples as lower confidence than the
   executed-converter examples.

8. The Unicode block table (section d) was assembled from Wikipedia/Wiktionary and
   confirmed via `unicodedata`; I did not fetch the raw unicode.org PDF chart
   (`U0980.pdf`). Confidence HIGH for the listed assigned points; the few rarely-used
   points (e.g. U+09FB GANDA, U+09F0/U+09F1 Assamese RA/WA, U+09F4–U+09F9 fraction/numeral
   signs) exist but were not central to this task.
