// FontRegistryTests.cs — xUnit tests for FontRegistry.Classify()
//
// Verifies font classification: Bijoy, Unicode, NonBengali, and Unsupported
// (unknown fonts that look Bangla-like).

namespace Mukti.Engine.Tests;

public sealed class FontRegistryTests
{
    private static readonly FontRegistry _registry = new();

    // -----------------------------------------------------------------------
    // Known Bijoy fonts
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData("sutonnymj")]
    [InlineData("SutonnyMJ")]           // uppercase
    [InlineData("SUTONNYMJ")]           // all caps
    [InlineData("SutonnyMJ Bold")]
    [InlineData("sutonnymj bold")]
    [InlineData("SutonnyMJ Italic")]
    [InlineData("  SutonnyMJ  ")]       // leading/trailing whitespace
    [InlineData("\tSutonnyMJ\t")]       // tab whitespace
    [InlineData("GangaMJ")]
    [InlineData("PadmaMJ")]
    [InlineData("JomunaMJ")]
    [InlineData("MeghnaMJ")]
    [InlineData("TeeshtaMJ")]
    [InlineData("TuragMJ")]
    [InlineData("SandipanMJ")]
    [InlineData("JugantorMJ")]
    [InlineData("SamakalMJ")]
    [InlineData("JaiJaiDinMJ")]
    public void Classify_KnownBijoyFont_ReturnsBijoy(string fontName)
    {
        var result = _registry.Classify(fontName);
        Assert.Equal(FontClass.Bijoy, result.Class);
        Assert.Equal(fontName, result.FontName);
        Assert.Null(result.Reason);
    }

    // -----------------------------------------------------------------------
    // Whitespace normalisation is applied to known Bijoy fonts
    // -----------------------------------------------------------------------

    [Fact]
    public void Classify_SutonnyMJWithLeadingTrailingWhitespace_ReturnsBijoy()
    {
        var result = _registry.Classify("  SutonnyMJ  ");
        Assert.Equal(FontClass.Bijoy, result.Class);
    }

    [Fact]
    public void Classify_SutonnyMJCaseInsensitive_ReturnsBijoy()
    {
        var result = _registry.Classify("SutonnyMJ");
        Assert.Equal(FontClass.Bijoy, result.Class);
    }

    // -----------------------------------------------------------------------
    // Known Unicode Bengali fonts
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData("SolaimanLipi")]
    [InlineData("solaimanlipi")]
    [InlineData("Noto Sans Bengali")]
    [InlineData("noto sans bengali")]
    [InlineData("Noto Serif Bengali")]
    [InlineData("Kohinoor Bangla")]
    [InlineData("Nikosh")]
    [InlineData("NikoshBan")]
    [InlineData("Kalpurush")]
    [InlineData("SutonnyOMJ")]         // OpenType variant — Unicode despite "MJ"-like name
    [InlineData("Shonar Bangla")]       // Windows built-in Unicode Bengali font
    [InlineData("shonar bangla")]
    public void Classify_KnownUnicodeFont_ReturnsUnicode(string fontName)
    {
        var result = _registry.Classify(fontName);
        Assert.Equal(FontClass.Unicode, result.Class);
        Assert.Null(result.Reason);
    }

    // -----------------------------------------------------------------------
    // Non-Bengali fonts (no Bengali marker)
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData("Times New Roman")]
    [InlineData("Arial")]
    [InlineData("Helvetica")]
    [InlineData("Calibri")]
    [InlineData("Georgia")]
    [InlineData("Verdana")]
    [InlineData("Comic Sans MS")]
    [InlineData("Courier New")]
    public void Classify_NonBengaliFont_ReturnsNonBengali(string fontName)
    {
        var result = _registry.Classify(fontName);
        Assert.Equal(FontClass.NonBengali, result.Class);
    }

    // -----------------------------------------------------------------------
    // Unsupported fonts — look Bangla-like but are not on the known list
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData("SomeFontMJ")]                    // "mj" marker
    [InlineData("Arial Bangla Unknown")]           // "bangla" marker
    [InlineData("BijoyExtra")]                     // "bijoy" marker
    [InlineData("BengaliScript Pro")]              // "bengali" marker
    [InlineData("UnknownLipi")]                    // "lipi" marker
    public void Classify_UnknownBanglaLookingFont_ReturnsUnsupported(string fontName)
    {
        var result = _registry.Classify(fontName);
        Assert.Equal(FontClass.Unsupported, result.Class);
        Assert.NotNull(result.Reason);
        Assert.NotEmpty(result.Reason);
    }

    // -----------------------------------------------------------------------
    // FontName is always preserved verbatim in the result
    // -----------------------------------------------------------------------

    [Fact]
    public void Classify_PreservesOriginalFontName()
    {
        const string originalName = "  SutonnyMJ  ";
        var result = _registry.Classify(originalName);
        Assert.Equal(originalName, result.FontName);
    }

    // -----------------------------------------------------------------------
    // SutonnyOMJ must NOT classify as Bijoy despite the "MJ"-like suffix
    // (confirmed false-positive, documented in finding 02-fonts.md).
    // -----------------------------------------------------------------------

    [Fact]
    public void Classify_SutonnyOMJ_IsUnicodeNotBijoy()
    {
        var result = _registry.Classify("SutonnyOMJ");
        Assert.Equal(FontClass.Unicode, result.Class);
        Assert.NotEqual(FontClass.Bijoy, result.Class);
    }

    // -----------------------------------------------------------------------
    // The known-list accessors are non-empty
    // -----------------------------------------------------------------------

    [Fact]
    public void KnownBijoy_IsNonEmpty()
    {
        Assert.NotEmpty(_registry.KnownBijoy);
    }

    [Fact]
    public void KnownUnicode_IsNonEmpty()
    {
        Assert.NotEmpty(_registry.KnownUnicode);
    }

    [Fact]
    public void BengaliMarkers_IsNonEmpty()
    {
        Assert.NotEmpty(_registry.BengaliMarkers);
    }

    // -----------------------------------------------------------------------
    // Newly added font names — spot-check at least 8 entries per group
    // -----------------------------------------------------------------------

    [Theory]
    // Proshika Multimedia
    [InlineData("proshika mn")]
    [InlineData("Proshika MN")]
    [InlineData("proshikamn")]
    [InlineData("proshika mt bold")]
    // Rupali
    [InlineData("rupali")]
    [InlineData("Rupali")]
    [InlineData("rupali mj")]
    [InlineData("rupali bold")]
    // Boishakhi/Baisakhi
    [InlineData("boishakhi")]
    [InlineData("baisakhi")]
    [InlineData("boishakhi mj")]
    [InlineData("baisakhi mj")]
    // Bashundhara
    [InlineData("bashundhara")]
    [InlineData("Bashundhara MJ")]
    [InlineData("bashundharamj")]
    [InlineData("bashumdhara")]
    // Additional Ananda Computers river-named fonts
    [InlineData("shitalakshyamj")]
    [InlineData("bhairakmj")]
    [InlineData("surmamj")]
    [InlineData("kushiyaramj")]
    [InlineData("dhaleshwarimj")]
    [InlineData("brahmaputramj")]
    [InlineData("teestamj")]
    [InlineData("burigangamj")]
    // Other Bijoy-family
    [InlineData("muktimj")]
    [InlineData("abasan mj")]
    [InlineData("mitramj")]
    [InlineData("shanta mj")]
    public void Classify_NewlyAddedBijoyFont_ReturnsBijoy(string fontName)
    {
        var result = _registry.Classify(fontName);
        Assert.Equal(FontClass.Bijoy, result.Class);
        Assert.Null(result.Reason);
    }

    // -----------------------------------------------------------------------
    // Regression guards — existing entries must still classify correctly
    // -----------------------------------------------------------------------

    [Fact]
    public void Classify_Nikosh_IsUnicode()
    {
        // Regression guard: "nikosh" must NOT be Bijoy or Unsupported
        var result = _registry.Classify("nikosh");
        Assert.Equal(FontClass.Unicode, result.Class);
    }

    [Fact]
    public void Classify_SiyamRupaliPlain_IsNonBengali()
    {
        // Plain "siyam rupali" (no ANSI) is a Unicode font but does not
        // appear on KnownUnicode list — classifies as NonBengali (no marker hit).
        var result = _registry.Classify("siyam rupali");
        Assert.Equal(FontClass.NonBengali, result.Class);
    }

    // -----------------------------------------------------------------------
    // "ansi" marker — unknown ANSI-suffixed Bengali fonts -> Unsupported
    // -----------------------------------------------------------------------

    [Fact]
    public void Classify_UnknownAnsiSuffixedFont_ReturnsUnsupported()
    {
        // "something ansi" is not on any known list but contains the "ansi"
        // marker, so it should surface as Unsupported rather than NonBengali.
        var result = _registry.Classify("something ansi");
        Assert.Equal(FontClass.Unsupported, result.Class);
        Assert.NotNull(result.Reason);
    }
}
