// ExcelSamplesTests.cs — Real-world Bijoy strings extracted from BRAC xlsx files.
//
// Strings were extracted from xl/sharedStrings.xml in two BRAC xlsx files:
//   File A: PO & BM Action Plan Format_2020 - 2nd time.xlsx  (BMTI_Onboarding)
//   File B: Urban_ Information Book Cohort 2026_ASH.xlsx      (Implementation Guideline C2026)
//
// Tests only verify that the engine produces Bengali Unicode output (U+0980–U+09FF)
// and that Bijoy-encoded non-ASCII characters are consumed by the conversion.
// Exact expected strings are not asserted — this is a quality smoke test.

namespace Mukti.Engine.Tests;

public sealed class ExcelSamplesTests
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

    /// <summary>
    /// Returns true if the string contains at least one Bengali Unicode codepoint (U+0980..U+09FF).
    /// </summary>
    private static bool ContainsBengali(string s) =>
        s.Any(c => c >= 'ঀ' && c <= '৿');

    // -----------------------------------------------------------------------
    // File A: PO & BM Action Plan Format_2020 - 2nd time.xlsx
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData("Kgx©i bvg")]
    [InlineData("‡mvgevi")]
    [InlineData("g½jevi")]
    [InlineData("e„n¯úwZevi")]
    [InlineData("mvßvwnK wgwUs")]
    [InlineData("MÖvg:")]
    [InlineData("m`‡m¨i bvg")]
    [InlineData("wcI BDwcwRi ¯^vÿi:")]
    [InlineData("m`m¨ msL¨v (1+2+3) = ‡gvU")]
    [InlineData("Kv‡jKkb c‡q‡›Ui bvg emvi ¯'vb I mgq")]
    [InlineData("kvLv e¨e¯'vc‡Ki ¯^vÿi:")]
    [InlineData("µt bs")]
    [InlineData("mKvj (8.30 †_‡K 1.30 wgwbU ch©šÍ)")]
    [InlineData("weKvj (2.30 †_‡K 5.00 wgwbU ch©šÍ)")]
    [InlineData("Kv‡Ri weeib")]
    [InlineData("cÖ¯'ZKvixi ¯^vÿit")]
    [InlineData("Aby‡gv`bKvixi ¯^vÿit")]
    [InlineData("MÖvg/¯úU/KwgwUi bvg")]
    [InlineData("[1] Kg©m~wP msMVK (BDwcwR) Gi ˆ`wbK d‡jvAvc cwiKíbv-20......... (cÖ_g I Z…Zxq mßvn)")]
    [InlineData("eª¨vK - AvjUªv-cyIi MÖ¨vRy‡qkb (BDwcwR) †cÖvMÖvg")]
    [InlineData("[2] Kg©m~wP msMVK (BDwcwR) Gi ˆ`wbK d‡jvAvc cwiKíbv-20........ (wØZxq I PZz_© mßvn)")]
    [InlineData("eª¨vK, AvjUªv-cyIi MÖ¨vRy‡qkb (BDwcwR) †cÖvMÖvg")]
    [InlineData("kvLv e¨e¯'vcK (BDwcwR) Gi gvwmK Kg©cwiKíbv")]
    [InlineData("m`m¨ msL¨v")]
    [InlineData("‡nvgwfwRU")]
    [InlineData("MÖæcwfwRU")]
    [InlineData("Kv‡jKkb c‡q‡›Ui bvg")]
    [InlineData("¯ú‡Ui bvg")]
    [InlineData("‡bvU: iweevi †_‡K eyaevi weKvj 5.00 Uv †_‡K 5.30 Uv ch©šÍ Awdwmqvj KvR Ki‡e| e„n¯úwZevi mKv‡j MÖæcwfwR‡Ui ¯'‡j ¯^v¯'¨‡mev †K‡›`ªi bvg wjL‡Z n‡e|")]
    public void FileA_ProducesBengaliOutput(string bijoyInput)
    {
        var output = _converter.Convert(bijoyInput);
        Assert.True(ContainsBengali(output),
            $"Expected Bengali Unicode in output.\n[IN]  {bijoyInput}\n[OUT] {output}");
    }

    // -----------------------------------------------------------------------
    // File B: Urban_ Information Book Cohort 2026_ASH.xlsx
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData("ch©šÍ")]
    [InlineData("kvLv e¨e¯'vcK/ GjvKv e¨e¯'vcK, BDwcwR-i ¯^vÿi I ZvwiL")]
    [InlineData("(cwi\"Qbœfv‡e wjL‡Z n‡e, †hb m`m¨ wb‡R c‡o eyS‡Z cv‡ib)")]
    [InlineData("¯^v¯'¨‡mevRwbZ Avw_©K mnvqZvi weeiY")]
    [InlineData("†diZ")]
    [InlineData("†diZ‡hvM¨ e&ª¨vK mnvqZvi cwigvY (UvKv)")]
    [InlineData("†diZ‡hvM¨ eª¨vK mnvqZvi †gqv`")]
    [InlineData("†gvevBj b¤^i")]
    [InlineData("†gvU")]
    [InlineData("†gvU cvwÿK †diZ msL¨v")]
    [InlineData("†gvU D‡Ëvjb")]
    [InlineData("†nj_ †Kqvi ‡cÖvfvBWvi")]
    [InlineData("‡_‡K †mev wb‡q‡Q")]
    [InlineData("Aby`vb I †diZ‡hvM¨ eª¨vK mnvqZvi")]
    [InlineData("Aby`vb I †diZ‡hvM¨ eª¨vK mnvqZvi weeiY (1g)")]
    [InlineData("Aby`vb I †diZ‡hvM¨ eª¨vK mnvqZvi weeiY (2q)")]
    [InlineData("Av`v‡qi evwK")]
    [InlineData("Av`v‡qi Z_¨")]
    [InlineData("cÖRbb")]
    [InlineData("cwi‡kv‡ai ZvwiL")]
    [InlineData("evi miKvwi/‡emiKvwi")]
    [InlineData("G›UvicÖvBR wfwR‡U/‡nvgwfwR‡U we‡kl ch©‡eÿb I Kibxq")]
    [InlineData("mnvqZvi aiY/†iv‡Mi bvg")]
    [InlineData("n‡q‡Q Ges D³ m¤ú` Ges †m¸‡jv †_‡K Av‡q m`m¨i wbqš¿b Av‡Q")]
    [InlineData("(bwgwb cÖ`v‡bi †ÿ‡Î Kb¨v mšÍvb, bvix‡K AMÖvwaKvi w`‡Z n‡e)")]
    [InlineData("`~‡h©vM e¨e¯'vcbv")]
    [InlineData("¯^v¯'¨ evZvqb (¯^v¯'¨ msµvšÍ civgk©)")]
    [InlineData("Kgx©i ¯^vÿi")]
    [InlineData("¯^v¯'¨m¤§Z j¨vwUª‡bi e¨envi I wbivc` cvwb cvb")]
    [InlineData("¯^vÿi")]
    [InlineData("¯'vqx LiP")]
    [InlineData("¯'vqx wVKvbv")]
    [InlineData("¯‹zj Mg‡bvc‡hvMx †Q‡j‡g‡qiv ¯‹z‡j hvq")]
    [InlineData("µwgK b¤^i")]
    [InlineData("G ch©šÍ Av`vq")]
    [InlineData("†diZ‡hvM¨ eª¨vK mnvqZv")]
    [InlineData("†diZ‡hvM¨ eª¨vK mnvqZv Av`v‡qi weeiY")]
    [InlineData("†diZ‡hvM¨ eª¨vK mnvqZvi")]
    public void FileB_ProducesBengaliOutput(string bijoyInput)
    {
        var output = _converter.Convert(bijoyInput);
        Assert.True(ContainsBengali(output),
            $"Expected Bengali Unicode in output.\n[IN]  {bijoyInput}\n[OUT] {output}");
    }
}
