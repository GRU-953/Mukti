// EdgeCaseTests.cs — Targeted tests for gaps not covered by the corpus or existing test files.
//
// Gaps covered:
//   1. Mixed Bijoy + Unicode Bengali in a single string: engine converts only the Bijoy
//      portion; the Unicode run passes through via the idempotency guard logic (IsBijoyText
//      fires on the whole string, so both halves go through MapGlyphs, but Unicode chars
//      not in the source-glyph table are copied verbatim).
//   2. ASCII punctuation only: none of .,!? are source glyphs — string passes through unchanged.
//   3. Non-breaking space (U+00A0) only: U+00A0 is not in the source-glyph table — returned as-is.
//   4. Hasanta (virama) sequence: '&' maps to ্ (U+09CD); 'K' maps to ক; 'K&K' should
//      produce ক্ক, which after NFC normalisation equals ক্ক.
//   5. Single-glyph ksha ligature: '¶' (U+00B6) maps directly to ক্ষ in one lookup.
//   6. IsBijoyText is true for inputs that contain a source glyph.
//   7. Bijoy digit conversion: '0'..'9' ARE source glyphs and map to Bengali digits ০-৯,
//      so a digit-only string is converted (not passed through).
//   8. Bijoy digit in a word: mixed ASCII letters + digits converts both portions correctly.

namespace Mukti.Engine.Tests;

public sealed class EdgeCaseTests
{
    private static readonly string SolutionRoot = FindSolutionRoot();
    private static readonly string DataFilePath = Path.Combine(SolutionRoot, "data", "bijoy-sutonnymj.json");

    private static string FindSolutionRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir, "Mukti.sln"))) return dir;
            dir = Path.GetDirectoryName(dir);
        }
        throw new DirectoryNotFoundException(
            "Cannot locate Mukti.sln — run tests from within the repository.");
    }

    private static readonly Converter _converter = new(new GlyphMap(DataFilePath));

    // -----------------------------------------------------------------------
    // 1. Mixed content: Bijoy glyph chars interleaved with Unicode Bengali chars.
    //    'K' is a Bijoy source glyph for ক; উ is already Unicode.
    //    IsBijoyText fires, so both chars go through MapGlyphs. 'K' maps to ক;
    //    উ is not a source key so it copies through verbatim.
    // -----------------------------------------------------------------------

    [Fact]
    public void Convert_MixedBijoyAndUnicode_BijoyGlyphsConverted()
    {
        // 'K' -> ক; 'উ' is already Unicode and not a source glyph -> stays উ
        var result = _converter.Convert("Kউ"); // 'K' + উ
        Assert.Equal("কউ", result);            // ক + উ
    }

    [Fact]
    public void Convert_MixedBijoyAndUnicode_UnicodeRunPreserved()
    {
        // The Unicode Bengali word বাংলা preceded by a Bijoy 'K'
        const string unicodePart = "বাংলা";
        var input = "K" + unicodePart;
        var result = _converter.Convert(input);
        Assert.Contains(unicodePart, result);
    }

    // -----------------------------------------------------------------------
    // 2. ASCII punctuation only: .,!? are not source glyphs.
    //    IsBijoyText returns false, so output is input.Normalize(NFC) == input.
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData(".,!?")]
    [InlineData("...")]
    [InlineData("!")]
    [InlineData("@#$%")]
    public void Convert_AsciiPunctuationOnly_PassesThroughUnchanged(string punct)
    {
        Assert.Equal(punct, _converter.Convert(punct));
    }

    // -----------------------------------------------------------------------
    // 3. Non-breaking space only (U+00A0).
    //    U+00A0 is not a source glyph; IsBijoyText returns false.
    // -----------------------------------------------------------------------

    [Fact]
    public void Convert_NonBreakingSpaceOnly_PassesThroughUnchanged()
    {
        const string nbsp = " ";
        Assert.Equal(nbsp, _converter.Convert(nbsp));
    }

    [Fact]
    public void IsBijoyText_NonBreakingSpaceOnly_ReturnsFalse()
    {
        Assert.False(_converter.IsBijoyText(" "));
    }

    // -----------------------------------------------------------------------
    // 4. Hasanta (virama) sequence.
    //    In the Bijoy glyph map:
    //      'K'  (U+004B) -> ক  (U+0995)
    //      '&'  (U+0026) -> ্  (U+09CD, BENGALI SIGN VIRAMA / hasanta)
    //    'K&' therefore produces ক্, which is ক followed by hasanta (a half-consonant
    //    cluster opener). 'K&K' produces ক্ক (ka-hasanta-ka = kka ligature).
    // -----------------------------------------------------------------------

    [Fact]
    public void Convert_KaFollowedByHasanta_ProducesKaWithVirama()
    {
        // 'K' + '&' -> ক + ্
        const string kaVirama = "ক্";
        Assert.Equal(kaVirama, _converter.Convert("K&"));
    }

    [Fact]
    public void Convert_KaHasantaKa_ProducesKkaLigature()
    {
        // 'K' + '&' + 'K' -> ক + ্ + ক  (kka cluster)
        const string kka = "ক্ক";
        Assert.Equal(kka, _converter.Convert("K&K"));
    }

    // -----------------------------------------------------------------------
    // 5. Single-glyph ksha ligature.
    //    '¶' (U+00B6) maps directly to ক্ষ in one greedy lookup.
    //    This exercises the multi-codepoint target from a single source byte.
    // -----------------------------------------------------------------------

    [Fact]
    public void Convert_SingleGlyphKsha_ProducesKshaLigature()
    {
        // ¶ (U+00B6) -> ক্ষ
        const string ksha = "ক্ষ"; // ক্ষ in NFC
        var result = _converter.Convert("¶");
        Assert.Equal(ksha.Normalize(System.Text.NormalizationForm.FormC), result);
    }

    [Fact]
    public void Convert_AlternateGlyphKsha_ProducesKshaLigature()
    {
        // 'ÿ' (U+00FF) also maps to ক্ষ
        const string ksha = "ক্ষ";
        var result = _converter.Convert("ÿ");
        Assert.Equal(ksha.Normalize(System.Text.NormalizationForm.FormC), result);
    }

    // -----------------------------------------------------------------------
    // 6. IsBijoyText fires for a known source glyph.
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData("K")]          // 'K' -> ক
    [InlineData("K&K")]        // 'K', '&', 'K' are all source glyphs
    [InlineData("¶")]     // ¶ is a source glyph
    public void IsBijoyText_SourceGlyphPresent_ReturnsTrue(string input)
    {
        Assert.True(_converter.IsBijoyText(input));
    }

    // -----------------------------------------------------------------------
    // 7. Digit-only input is converted (digits ARE Bijoy source glyphs).
    //    '0'-'9' in Bijoy map to Bengali digits ০-৯ (U+09E6..U+09EF).
    //    This is a deliberate contrast with the "digits pass through" assumption.
    // -----------------------------------------------------------------------

    [Fact]
    public void Convert_DigitOnly_ProducesBengaliDigits()
    {
        // Bijoy '0'..'9' map to ০..৯
        Assert.Equal("০১২৩৪৫৬৭৮৯", _converter.Convert("0123456789"));
    }

    [Fact]
    public void IsBijoyText_DigitsOnly_ReturnsTrue()
    {
        // Digits ARE source glyphs in the Bijoy glyph map.
        Assert.True(_converter.IsBijoyText("123"));
    }

    // -----------------------------------------------------------------------
    // 8. Bijoy digit embedded in a Bijoy word — both digit and letter glyphs
    //    convert in the same pass.
    //    'K' -> ক; '1' -> ১
    // -----------------------------------------------------------------------

    [Fact]
    public void Convert_BijoyLetterAndDigitMixed_BothConverted()
    {
        // 'K' -> ক, '1' -> ১
        Assert.Equal("ক১", _converter.Convert("K1"));
    }
}
