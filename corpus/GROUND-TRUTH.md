# Ground Truth — how expected outputs are justified

Expected Unicode in this corpus is anchored to **independent, authoritative
sources** and to verifiable Unicode rules — never to the converter under test,
and never to the suspect prior-version map.

Two layers of justification:

1. **The SutonnyMJ/Bijoy input → Bengali mapping** (which keystroke bytes mean
   which Bengali component) — anchored to cited external references. See
   [`../docs/phase0/findings/06-mapping-research.md`](../docs/phase0/findings/06-mapping-research.md).
2. **Correct Unicode storage form (NFC)** — anchored to the Unicode Standard and
   verified mechanically (commands below are reproducible with `node`).

---

## Verified Unicode / NFC rules (mechanically confirmed)

These were confirmed on this machine with `String.prototype.normalize('NFC')`
(Node 22). They are *requirements* on every `expected` value and on every
converter output.

### R1 — Nukta letters are composition exclusions → store DECOMPOSED in NFC

Bengali ‌য়, ড়, ঢ় must be stored as **base consonant + nukta (U+09BC)**, not as
their single precomposed code points. NFC actively decomposes the precomposed
forms:

| Glyph | WRONG (precomposed, not NFC) | CORRECT (NFC) |
|---|---|---|
| ড় | U+09DC | U+09A1 U+09BC |
| ঢ় | U+09DD | U+09A2 U+09BC |
| য় | U+09DF | U+09AF U+09BC |

```
node -e 'console.log(Array.from("য়".normalize("NFC")).map(c=>c.codePointAt(0).toString(16)))'
# -> [ '9af', '9bc' ]
```

> The prior engine named a constant `NUKTA` but pointed it at U+0981
> (chandrabindu), so its ড়/ঢ়/য় output was almost certainly wrong. This is a
> headline reason the corpus exists.

### R2 — Pre-base vowel signs are stored AFTER their consonant

The vowel signs ি (U+09BF), ে (U+09C7), ৈ (U+09C8) are *drawn* to the left of
their consonant but *stored* after it. Bijoy stores them in visual (drawing)
order, so conversion must reorder. Example: `কি` is `U+0995 U+09BF` (ka + i-kar),
never `U+09BF U+0995`.

### R3 — Two-part vowel signs ো ৌ

ো (o-kar) = U+09C7 U+09BE and ৌ (au-kar) = U+09C7 U+09D7. In a correct Unicode
string these appear as the two parts after the consonant; NFC does **not**
merge them into a single code point (there is none). Cases storing these must
use the two-part sequences.

### R4 — Reph (র্ before a consonant)

Where Bijoy places a reph mark *before* the consonant cluster it belongs to, the
Unicode storage is `র (U+09B0) + ্ (U+09CD) + <consonant>`. Example: `র্ক` =
U+09B0 U+09CD U+0995.

### R5 — Conjuncts use the virama/hosonto (U+09CD)

A conjunct is `<consonant> + U+09CD + <consonant>`. Example: ক্ত =
U+0995 U+09CD U+09A4.

### R6 — Khanda-ta is its own code point

ৎ (khanda ta) = U+09CE — not ত + virama. Cases must use U+09CE.

### Bengali block reference (U+0980–U+09FF)

Authoritative table to be quoted from the Unicode Standard in
`findings/06-mapping-research.md`. Key points used above:
U+0981 ঁ chandrabindu · U+0982 ং anusvara · U+0983 ঃ visarga ·
U+09BC ় nukta · U+09BE া · U+09BF ি · U+09C0 ী · U+09C7 ে · U+09C8 ৈ ·
U+09CB ো · U+09CC ৌ · U+09CD ্ virama · U+09CE ৎ khanda-ta ·
U+09D7 ৗ au-length-mark · digits ০–৯ U+09E6–U+09EF.

---

## Mapping table (from cited research)

> Filled in from [`findings/06-mapping-research.md`](../docs/phase0/findings/06-mapping-research.md)
> once the research pass lands. Each corpus case's `ref` points to the row or
> worked example that justifies it. Cases the maintainer verifies by hand are
> marked `maintainer-verified`.
