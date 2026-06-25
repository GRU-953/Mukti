// Reorder.cs — Port of reorder.ts
// Bengali cluster reordering — three algorithmic passes applied after the flat
// glyph substitution step.
//
// Bijoy stores some marks in display order; Unicode stores them in logical order.
// After substitution we must:
//   1. Collapse an accidental double virama (e.g. স্ + ্ব -> স্ব).
//   2. Move reph (র্) to the start of its consonant cluster.
//   3. Move pre-base vowel signs (drawn to the left: ি ে ৈ) after their
//      consonant unit.
//
// Unicode constants used:
//   VIRAMA  U+09CD  ্
//   NUKTA   U+09BC  ়
//   ি       U+09BF  i-kar
//   ে       U+09C7  e-kar
//   ৈ       U+09C8  ai-kar
//   ক       U+0995  first consonant in Bengali block
//   হ       U+09B9  last consonant in Bengali block
//   র       U+09B0  ra (used in reph র্)
//
// Pure string logic; no I/O. All regexes are engine-controlled (not
// user-supplied) and fixed at compile time — safe from ReDoS.

using System.Text.RegularExpressions;

namespace Mukti.Engine;

/// <summary>
/// Applies Bengali cluster reordering passes to already glyph-substituted text.
/// </summary>
public static partial class Reorder
{
    // Unicode scalar string constants — kept as string literals for readability.
    // VIRAMA U+09CD
    private const string Virama = "্";
    // NUKTA U+09BC
    private const string Nukta = "়";
    // Reph = র (U+09B0) + ্ (U+09CD)
    private const string RephStr = "র্";

    // Bengali consonant block range: ক (U+0995) .. হ (U+09B9).
    // Used inside regex character class brackets: [ক-হ]
    private const string ConsClass = @"[ক-হ]";

    // Pre-base vowel signs (drawn to the left of their consonant in Bijoy display order):
    //   ি U+09BF  i-kar
    //   ে U+09C7  e-kar
    //   ৈ U+09C8  ai-kar
    private const string PreBaseClass = @"[িেৈ]";

    // A "consonant unit": consonant, optional nukta, then zero or more (virama+consonant[+nukta]) pairs.
    // Pattern: [ক-হ]়?(?:্[ক-হ]়?)*
    private const string CUnitPattern =
        @"[ক-হ]়?(?:্[ক-হ]়?)*";

    // -----------------------------------------------------------------------
    // Compiled regexes — instantiated once per process. Using [GeneratedRegex]
    // source generator for .NET 7+ AOT-safe compiled patterns.
    // -----------------------------------------------------------------------

    // Pass 1: double virama  ্্  ->  ্
    [GeneratedRegex("্্", RegexOptions.Compiled)]
    private static partial Regex DoubleViramaRegex();

    // Pass 2: reph reordering
    // Matches: (any Bengali consonant)(র্)
    // Replaces with: র্(consonant)
    // i.e. the reph marker stored AFTER the consonant moves BEFORE it.
    [GeneratedRegex(@"([ক-হ])র্", RegexOptions.Compiled)]
    private static partial Regex RephRegex();

    // Pass 3: pre-base vowel reordering
    // Matches: (pre-base vowel sign)(consonant unit)
    // Replaces with: (consonant unit)(pre-base vowel sign)
    [GeneratedRegex(
        @"([িেৈ])([ক-হ]়?(?:্[ক-হ]়?)*)",
        RegexOptions.Compiled)]
    private static partial Regex PreBaseRegex();

    /// <summary>
    /// Apply the three Bengali reordering passes to already glyph-substituted text.
    /// </summary>
    /// <param name="input">Text after glyph map substitution.</param>
    /// <returns>Text with Bengali clusters in Unicode logical order.</returns>
    public static string Apply(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        // Pass 1: collapse accidental double virama
        var s = DoubleViramaRegex().Replace(input, Virama);

        // Pass 2: reph — consonant immediately followed by র্ -> র্ moves before that consonant
        s = RephRegex().Replace(s, RephStr + "$1");

        // Pass 3: pre-base vowels -> moved after their consonant unit
        s = PreBaseRegex().Replace(s, "$2$1");

        return s;
    }
}
