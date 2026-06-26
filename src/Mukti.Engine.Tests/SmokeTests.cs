// SmokeTests.cs — End-to-end smoke tests for gru953-markdown real-world conversion cases.
//
// Covers: vowel combinations, single words, conjuncts, reph+pre-kar interaction,
// chandrabindu, special characters, complex BRAC corpus entries, and already-Unicode
// passthrough. Uses [Theory][InlineData] throughout for dense coverage with clear diffs on failure.

namespace Mukti.Engine.Tests;

public sealed class SmokeTests
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
    // Vowel combinations
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData("Av",   "আ")]        // independent aa vowel via multi-char
    [InlineData("†Kv",  "কো")]       // composite o-kar: e-kar + ka + aa-kar
    [InlineData("†M",   "গে")]       // simple pre-kar reorder: e-kar + ga
    [InlineData("wK",   "কি")]       // i-kar pre-base reorder
    [InlineData("K~",   "কূ")]       // uu-kar
    [InlineData("K„",   "কৃ")]       // ri-kar
    public void Convert_VowelCombinations(string input, string expected)
    {
        Assert.Equal(expected, _converter.Convert(input));
    }

    // -----------------------------------------------------------------------
    // Single words
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData("evsjv",       "বাংলা")]       // Bangla
    [InlineData("Avwg",        "আমি")]         // I/me
    [InlineData("evsjv‡`k",   "বাংলাদেশ")]    // Bangladesh
    [InlineData("eÜz",         "বন্ধু")]       // friend — na+virama+dha+u-kar
    [InlineData("gyw³",        "মুক্তি")]      // mukti — ka+virama, u-kar, ti with i-kar
    [InlineData("wkÿv",        "শিক্ষা")]      // education — ksha conjunct
    [InlineData("b`x",         "নদী")]         // river — pre-base i-kar
    public void Convert_SingleWords(string input, string expected)
    {
        Assert.Equal(expected, _converter.Convert(input));
    }

    // -----------------------------------------------------------------------
    // Conjuncts
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData("gš¿x",      "মন্ত্রী")]      // triple conjunct: na+virama+ta+virama+ra with reph + i-kar
    [InlineData("cÖ_g",      "প্রথম")]        // first — ra-phala
    [InlineData("¯^vaxbZv",  "স্বাধীনতা")]    // independence — sa+virama+ba
    [InlineData("MÖvg",      "গ্রাম")]        // village — ra-phala with aa-kar
    [InlineData("ag©",       "ধর্ম")]         // dharma — reph on ma
    [InlineData("c~Y©",      "পূর্ণ")]        // complete — reph on nna
    public void Convert_Conjuncts(string input, string expected)
    {
        Assert.Equal(expected, _converter.Convert(input));
    }

    // -----------------------------------------------------------------------
    // Reph + pre-kar interaction
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData("wbg©vY", "নির্মাণ")]   // construction — pre-base i-kar + reph on ma
    public void Convert_RephPreKarInteraction(string input, string expected)
    {
        Assert.Equal(expected, _converter.Convert(input));
    }

    // -----------------------------------------------------------------------
    // Chandrabindu
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData("Pvu`", "চাঁদ")]   // moon — chandrabindu mid-word
    public void Convert_Chandrabindu(string input, string expected)
    {
        Assert.Equal(expected, _converter.Convert(input));
    }

    // -----------------------------------------------------------------------
    // Special characters and real-world forms
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData("nVvr",  "হঠাৎ")]   // suddenly — with khanda-ta
    [InlineData("`ytL",  "দুঃখ")]   // sorrow — visarga
    [InlineData("1971",  "১৯৭১")]   // year in Bengali digits
    public void Convert_SpecialCharactersAndRealWorld(string input, string expected)
    {
        Assert.Equal(expected, _converter.Convert(input));
    }

    // -----------------------------------------------------------------------
    // Complex BRAC corpus entries
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData("cÖwZÔvb",   "প্রতিষ্ঠান")]   // institution — ra-phala + ssa conjunct
    [InlineData("we¯ÍvwiZ",  "বিস্তারিত")]    // detailed
    [InlineData("Kg©KvÐ",    "কর্মকাণ্ড")]    // activities
    public void Convert_BracCorpusEntries(string input, string expected)
    {
        Assert.Equal(expected, _converter.Convert(input));
    }

    // -----------------------------------------------------------------------
    // Already-Unicode passthrough — engine must be a no-op for Unicode Bengali
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData("আমি")]
    [InlineData("বাংলাদেশ")]
    public void Convert_AlreadyUnicode_PassesThroughUnchanged(string input)
    {
        Assert.Equal(input, _converter.Convert(input));
    }
}
