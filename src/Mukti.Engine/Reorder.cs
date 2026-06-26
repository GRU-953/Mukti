// Reorder.cs — Full port of _rearrange() from bijoy_unicode.py
// Bengali cluster reordering — two algorithmic passes applied after the flat
// glyph substitution step.
//
// Bijoy stores some marks in display order; Unicode stores them in logical order.
// After substitution we must:
//   Preamble — collapse double virama (্্ -> ্)
//   Pass 1   — reph repositioning + halant/vowel reordering
//   Pass 2   — pre-kar repositioning + composite vowel formation + nukta swap
//
// Unicode constants used:
//   VIRAMA          U+09CD  ্
//   CHANDRABINDU    U+0981  ঁ  (called "nukta" in the reference algorithm)
//   ি               U+09BF  i-kar
//   ে               U+09C7  e-kar
//   ৈ               U+09C8  ai-kar
//   া               U+09BE  aa-kar
//   ৗ               U+09D7  au-length mark
//   ো               U+09CB  o-kar  (composite: ে + া)
//   ৌ               U+09CC  au-kar (composite: ে + ৗ)
//   র               U+09B0  ra (used in reph র্)
//
// Pure string logic; no I/O.

namespace Mukti.Engine;

/// <summary>
/// Applies Bengali cluster reordering passes to already glyph-substituted text.
/// </summary>
public static class Reorder
{
    // ── Character classification sets ────────────────────────────────────────

    private static readonly HashSet<char> Consonants = new(
        "কখগঘঙচছজঝঞটঠডঢণতথদধনপফবভমযরলশষসহড়ঢ়য়ৎংঃঁ");

    private static readonly HashSet<char> PreKars = new("িেৈ");

    private static readonly HashSet<char> PostKars = new("াোৌৗুূীৃ");

    // AllKars = PreKars ∪ PostKars
    private static readonly HashSet<char> AllKars = new("িেৈাোৌৗুূীৃ");

    private const char Virama = '্';  // U+09CD

    // Called "nukta" in the reference algorithm; actually chandrabindu U+0981
    private const char Nukta = 'ঁ';   // U+0981

    private const char Ra = 'র';      // U+09B0

    // ── Bounds-safe character accessor ───────────────────────────────────────

    private static char Ch(string s, int i) =>
        i >= 0 && i < s.Length ? s[i] : '\0';

    // ── Public API ───────────────────────────────────────────────────────────

    /// <summary>
    /// Apply Bengali cluster reordering to already glyph-substituted text.
    /// </summary>
    /// <param name="input">Text after glyph map substitution.</param>
    /// <returns>Text with Bengali clusters in Unicode logical order.</returns>
    public static string Apply(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        // Preamble: collapse double virama  ্্  ->  ্
        var text = input.Replace("্্", "্");

        // ── Pass 1: reph and halant reordering ───────────────────────────────
        var i = 0;
        while (i < text.Length)
        {
            // 1a. Reph repositioning:
            //     র + ্ (not preceded by ্) -> move র্ before cluster start.
            if (i > 0 && i < text.Length - 1 &&
                Ch(text, i) == Ra &&
                Ch(text, i + 1) == Virama &&
                Ch(text, i - 1) != Virama)
            {
                // Walk backward past any kars to find the consonant base.
                var check = i - 1;
                while (check >= 0 && AllKars.Contains(Ch(text, check)))
                    check--;

                if (check >= 0 && Consonants.Contains(Ch(text, check)))
                {
                    // Walk back through virama+consonant pairs to cluster start.
                    var clusterStart = check;
                    while (true)
                    {
                        if (clusterStart - 1 < 0) break;
                        if (Ch(text, clusterStart - 1) == Virama &&
                            clusterStart - 2 >= 0 &&
                            Consonants.Contains(Ch(text, clusterStart - 2)))
                        {
                            clusterStart -= 2;
                        }
                        else break;
                    }
                    text = text[..clusterStart] + Ra + Virama +
                           text[clusterStart..i] + text[(i + 2)..];
                    i = clusterStart + 2;
                    continue;
                }
            }

            // 1b. Vowel sign / nukta + virama + consonant
            //     -> virama + consonant + vowel sign
            if (i > 0 &&
                Ch(text, i) == Virama &&
                (AllKars.Contains(Ch(text, i - 1)) || Ch(text, i - 1) == Nukta) &&
                i < text.Length - 1)
            {
                text = text[..(i - 1)] + Ch(text, i).ToString() +
                       Ch(text, i + 1).ToString() + Ch(text, i - 1).ToString() +
                       text[(i + 2)..];
            }

            // 1c. RA + virama + vowel sign
            //     -> vowel sign + RA + virama
            if (i > 0 && i < text.Length - 1 &&
                Ch(text, i) == Virama &&
                Ch(text, i - 1) == Ra &&
                Ch(text, i - 2) != Virama &&
                AllKars.Contains(Ch(text, i + 1)))
            {
                text = text[..(i - 1)] + Ch(text, i + 1).ToString() +
                       Ch(text, i - 1).ToString() + Ch(text, i).ToString() +
                       text[(i + 2)..];
            }

            i++;
        }

        // ── Pass 2: pre-kar repositioning and composite vowels ───────────────
        i = 0;
        while (i < text.Length)
        {
            // 2a. Pre-kar repositioning spanning conjuncts, with composite vowel
            //     formation for ো (ে+া) and ৌ (ে+ৗ).
            if (i < text.Length - 1 &&
                PreKars.Contains(Ch(text, i)) &&
                !IsSpace(Ch(text, i + 1)))
            {
                // Walk j forward over consonant+(virama+consonant) pairs.
                var j = 1;
                while (i + j < text.Length - 1 &&
                       Consonants.Contains(Ch(text, i + j)))
                {
                    if (Ch(text, i + j + 1) == Virama)
                        j += 2;
                    else
                        break;
                }

                // Build: everything before pre-kar + consonant unit (excluding pre-kar).
                var basePart = text[..i] + text[(i + 1)..(i + j + 1)];
                var l = 0;
                var kar = Ch(text, i);
                var nxt = Ch(text, i + j + 1);

                if (kar == 'ে' && nxt == 'া')
                {
                    basePart += 'ো'; l = 1;
                }
                else if (kar == 'ে' && nxt == 'ৗ')
                {
                    basePart += 'ৌ'; l = 1;
                }
                else
                {
                    basePart += kar;
                }

                text = basePart + text[(i + j + l + 1)..];
                i += j;
                // fall through to 2b check at updated i
            }

            // 2b. Nukta + post-kar swap.
            if (i < text.Length - 1 &&
                Ch(text, i) == Nukta &&
                PostKars.Contains(Ch(text, i + 1)))
            {
                text = text[..i] + Ch(text, i + 1).ToString() +
                       Ch(text, i).ToString() + text[(i + 2)..];
            }

            i++;
        }

        return text;
    }

    private static bool IsSpace(char c) => c is ' ' or '\t' or '\n' or '\r';
}
