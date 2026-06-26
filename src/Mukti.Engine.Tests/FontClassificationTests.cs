// FontClassificationTests.cs — Regression tests proving Mukti's font classifier
// converts ONLY Bijoy-family fonts and leaves every non-Bijoy font (both
// Unicode-Bengali and English) unchanged.
//
// Safety contract under test:
//   - Only curated Bijoy/SutonnyMJ-family names classify as FontClass.Bijoy.
//   - No Unicode Bengali font (including the documented "NikoshMJ" false-positive
//     and other empirically-Unicode MJ-suffixed fonts) is ever Bijoy.
//   - English / Latin fonts classify as FontClass.NonBengali.
//   - Plain "Siyam Rupali" is NOT Bijoy, but the "Siyam Rupali ANSI" build IS.
//   - Engine-level idempotency: already-Unicode Bengali passes through unchanged.

using System.IO;

namespace Mukti.Engine.Tests;

public sealed class FontClassificationTests
{
    private static readonly FontRegistry _reg = new();

    // -----------------------------------------------------------------------
    // A) Bijoy-family fonts classify as FontClass.Bijoy.
    //    Covers case-insensitivity, comma style suffixes ("SutonnyMJ,Bold"),
    //    hyphen/concatenated style variants, and the Siyam Rupali ANSI build.
    // -----------------------------------------------------------------------
    [Theory]
    [InlineData("SutonnyMJ")]
    [InlineData("sutonnymj")]
    [InlineData("SutonnyMJ,Bold")]
    [InlineData("SutonnyMJ-Regular")]
    [InlineData("SutonnyMJBold")]
    [InlineData("SutonnyCMJ")]
    [InlineData("SutonnySushreeMJ")]
    [InlineData("JomunaMJ")]
    [InlineData("SamakalMJ")]
    [InlineData("Siyam Rupali ANSI")]
    [InlineData("siyam rupali ansi")]
    [InlineData("ArhialkhanMJ")]    // confirmed Bijoy: FY 25-26 BRAC/UPG documents
    [InlineData("TangonMotaMJ")]    // confirmed Bijoy: Enterprise Training module
    public void BijoyFonts_ClassifyAsBijoy(string fontName)
    {
        Assert.Equal(FontClass.Bijoy, _reg.Classify(fontName).Class);
    }

    // -----------------------------------------------------------------------
    // B) Non-Bijoy Bengali Unicode fonts are NEVER FontClass.Bijoy.
    //    Includes the documented "NikoshMJ" false-positive and other MJ-suffixed
    //    names that are empirically Unicode despite the MJ suffix.
    // -----------------------------------------------------------------------
    [Theory]
    [InlineData("Kalpurush")]
    [InlineData("Nikosh")]
    [InlineData("NikoshBAN")]
    [InlineData("SolaimanLipi")]
    [InlineData("Kohinoor Bangla")]
    [InlineData("Shonar Bangla")]
    [InlineData("Vrinda")]
    [InlineData("Hind Siliguri")]
    [InlineData("Siyam Rupali")]
    [InlineData("Noto Sans Bengali")]
    [InlineData("NikoshMJ")]        // documented false-positive — must NOT be Bijoy
    [InlineData("SonkhoMJ")]
    // NOTE: ArhialkhanMJ and TangonMotaMJ were here, but are now confirmed Bijoy
    //       (found in real BRAC/UPG FY 25-26 documents) and have been moved to the
    //       known-Bijoy list in FontRegistry.cs and the Bijoy test theory above.
    public void NonBijoyBengaliUnicodeFonts_AreNeverBijoy(string fontName)
    {
        Assert.NotEqual(FontClass.Bijoy, _reg.Classify(fontName).Class);
    }

    // -----------------------------------------------------------------------
    // C) English / Latin fonts classify as FontClass.NonBengali.
    //    "Calibri, sans-serif" exercises the comma-suffix drop in normalization.
    // -----------------------------------------------------------------------
    [Theory]
    [InlineData("Arial")]
    [InlineData("Times New Roman")]
    [InlineData("Calibri")]
    [InlineData("Courier New")]
    [InlineData("Calibri, sans-serif")]
    [InlineData("Verdana")]
    public void EnglishFonts_ClassifyAsNonBengali(string fontName)
    {
        Assert.Equal(FontClass.NonBengali, _reg.Classify(fontName).Class);
    }

    // -----------------------------------------------------------------------
    // D) The ANSI-only distinction: plain "Siyam Rupali" is NOT Bijoy, but the
    //    "Siyam Rupali ANSI" legacy build IS Bijoy.
    // -----------------------------------------------------------------------
    [Fact]
    public void SiyamRupali_OnlyAnsiVariantIsBijoy()
    {
        Assert.NotEqual(FontClass.Bijoy, _reg.Classify("Siyam Rupali").Class);
        Assert.Equal(FontClass.Bijoy, _reg.Classify("Siyam Rupali ANSI").Class);
    }

    // -----------------------------------------------------------------------
    // E) Engine-level safety contract (idempotency guard):
    //    A Converter built from data/bijoy-sutonnymj.json, given already-Unicode
    //    Bengali text, returns it UNCHANGED. Loaded the same way as EdgeCaseTests:
    //    find Mukti.sln, then Path.Combine the data dir.
    // -----------------------------------------------------------------------

    private static readonly string SolutionRoot = FindSolutionRoot();
    private static readonly string DataFilePath = Path.Combine(SolutionRoot, "data", "bijoy-sutonnymj.json");
    private static readonly Converter _converter = new(new GlyphMap(DataFilePath));

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

    [Fact]
    public void Convert_AlreadyUnicodeBengali_ReturnsUnchanged()
    {
        const string unicodeBengali = "আমি বাংলায় কথা বলি";
        Assert.Equal(unicodeBengali, _converter.Convert(unicodeBengali));
    }
}
