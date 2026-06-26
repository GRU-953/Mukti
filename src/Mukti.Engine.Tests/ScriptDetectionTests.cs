// ScriptDetectionTests.cs — Tests for Converter.DetectScript (content-based script detection).
//
// The algorithm counts characters in three buckets:
//   bn  — Unicode Bengali (U+0980–U+09FF)
//   bj  — Bijoy/Latin-1-Supplement ranges (U+00A0–U+00FF, U+0152–U+0178,
//           U+2013–U+2122, and five individual codepoints)
//   la  — plain ASCII letters (U+0021–U+007F where IsLetter is true)
//
// Decision tree:
//   total == 0                           -> Other
//   bj >= 5 && bn == 0 && (la==0 || bj*10>=la) -> Bijoy
//   bn > 0                               -> UnicodeBn
//   else                                 -> Latin

namespace Mukti.Engine.Tests;

public sealed class ScriptDetectionTests
{
    [Theory]
    // -----------------------------------------------------------------------
    // Bijoy: ≥5 chars in Bijoy codepoint ranges, no Unicode Bengali,
    //        not dominated by plain ASCII letters.
    //   U+00B0 (°, DEGREE SIGN) is in 0x00A0–0x00FF (Bijoy range).
    // -----------------------------------------------------------------------

    // Five degree-sign chars → bj=5, bn=0, la=0 → Bijoy
    [InlineData("°°°°°", ScriptType.Bijoy)]

    // Ten degree-sign chars + one ASCII letter 'H' →
    //   bj=10, bn=0, la=1 → bj*10=100 >= la=1 → Bijoy
    [InlineData("°°°°°°°°°° H", ScriptType.Bijoy)]

    // Six chars from Latin Extended-A subset (U+0152–U+0178) — Œ is U+0152 —
    // all in Bijoy range: bj=6, bn=0, la=0 → Bijoy
    [InlineData("ŒŒŒŒŒŒ", ScriptType.Bijoy)]

    // -----------------------------------------------------------------------
    // Unicode Bengali: any U+0980–U+09FF character present → UnicodeBn
    //   (even if Bijoy-range chars are also present, bn>0 wins over Bijoy
    //    branch because the Bijoy branch requires bn==0)
    // -----------------------------------------------------------------------
    [InlineData("আমি বাংলায় লিখি", ScriptType.UnicodeBn)]
    [InlineData("ক", ScriptType.UnicodeBn)]
    [InlineData("বাংলাদেশ", ScriptType.UnicodeBn)]

    // -----------------------------------------------------------------------
    // Latin: ASCII letters only, no Bijoy-range or Bengali chars
    // -----------------------------------------------------------------------
    [InlineData("Hello world", ScriptType.Latin)]
    [InlineData("Python", ScriptType.Latin)]

    // ASCII Bijoy-font notation strings: these ARE plain ASCII (codepoints < 128)
    // so they are counted as Latin, not Bijoy.
    [InlineData("evsjv", ScriptType.Latin)]
    [InlineData("Avwg evsjvq wjwL", ScriptType.Latin)]

    // -----------------------------------------------------------------------
    // Other: no classifiable characters at all
    // -----------------------------------------------------------------------
    [InlineData("", ScriptType.Other)]
    [InlineData("12345", ScriptType.Other)]

    // -----------------------------------------------------------------------
    // Edge: single Bijoy char, below threshold (bj=1 < 5)
    //   bn=0, la=0 → total=1; Bijoy branch skipped; UnicodeBn skipped → Latin
    // -----------------------------------------------------------------------
    [InlineData("°", ScriptType.Latin)]

    // Edge: 1 Bijoy char + 5 ASCII letters → bj=1 < 5 threshold → Latin
    [InlineData("° Hello", ScriptType.Latin)]

    // Edge: 4 Bijoy chars, just below threshold → Latin
    [InlineData("°°°°", ScriptType.Latin)]
    public void DetectScript_ReturnsExpectedScriptType(string input, ScriptType expected)
    {
        Assert.Equal(expected, Converter.DetectScript(input));
    }
}
