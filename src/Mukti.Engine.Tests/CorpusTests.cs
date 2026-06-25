// CorpusTests.cs — xUnit parameterised tests driven by the JSONL corpus.
//
// Reads every *.jsonl file from both the visible and heldout corpus directories.
// Each test case calls Converter.Convert(sourceKeys) and asserts the result
// equals the expected Unicode string.
//
// Additional property-based tests:
//   - Idempotency:   Convert(Convert(x)) == Convert(x)
//   - URL passthrough: a URL embedded in Bijoy text is preserved unchanged
//   - Already-Unicode no-op: Convert on pure Unicode text returns the same string

using System.Text.Json;

namespace Mukti.Engine.Tests;

public sealed class CorpusTests
{
    // Resolve paths relative to the repo root at runtime so tests pass on any
    // machine (local dev or CI). Walks up from the test output directory until
    // it finds Mukti.sln.
    private static readonly string SolutionRoot = FindSolutionRoot();
    private static readonly string DataFilePath = Path.Combine(SolutionRoot, "data", "bijoy-sutonnymj.json");
    private static readonly string CorpusBase = Path.Combine(SolutionRoot, "corpus", "corpus");

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

    // -----------------------------------------------------------------------
    // Shared converter instance — loaded once for the whole test class.
    // -----------------------------------------------------------------------
    private static readonly Converter _converter = CreateConverter();

    private static Converter CreateConverter()
    {
        var glyphMap = new GlyphMap(DataFilePath);
        return new Converter(glyphMap);
    }

    // -----------------------------------------------------------------------
    // Corpus case record — mirrors the JSONL schema.
    // -----------------------------------------------------------------------
    private sealed record CorpusCase(
        string Id,
        string SourceKeys,
        string Expected,
        string Description,
        string File);

    // -----------------------------------------------------------------------
    // Load all corpus cases from all JSONL files.
    // -----------------------------------------------------------------------
    private static List<CorpusCase> LoadAllCases()
    {
        var cases = new List<CorpusCase>();

        var corpusBase = CorpusBase;

        // Visible corpus
        var visibleDir = Path.Combine(corpusBase, "visible");
        if (Directory.Exists(visibleDir))
        {
            foreach (var file in Directory.GetFiles(visibleDir, "*.jsonl").OrderBy(f => f))
                cases.AddRange(ParseJsonl(file));
        }

        // Heldout corpus
        var heldoutDir = Path.Combine(corpusBase, "heldout");
        if (Directory.Exists(heldoutDir))
        {
            foreach (var file in Directory.GetFiles(heldoutDir, "*.jsonl").OrderBy(f => f))
                cases.AddRange(ParseJsonl(file));
        }

        return cases;
    }

    private static IEnumerable<CorpusCase> ParseJsonl(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        foreach (var line in File.ReadLines(filePath, System.Text.Encoding.UTF8))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed)) continue;

            using var doc = JsonDocument.Parse(trimmed);
            var root = doc.RootElement;

            var id = root.TryGetProperty("id", out var idProp) ? idProp.GetString() ?? "" : "";
            var sourceKeys = root.TryGetProperty("sourceKeys", out var skProp) ? skProp.GetString() ?? "" : "";
            var expected = root.TryGetProperty("expected", out var exProp) ? exProp.GetString() ?? "" : "";
            var description = root.TryGetProperty("description", out var descProp) ? descProp.GetString() ?? "" : "";

            yield return new CorpusCase(id, sourceKeys, expected, description, fileName);
        }
    }

    // -----------------------------------------------------------------------
    // MemberData source — all corpus cases as xUnit theory rows.
    // -----------------------------------------------------------------------
    public static IEnumerable<object[]> AllCorpusCases()
    {
        foreach (var c in LoadAllCases())
            yield return new object[] { c.Id, c.SourceKeys, c.Expected, c.Description };
    }

    // -----------------------------------------------------------------------
    // MemberData for idempotency tests.
    // -----------------------------------------------------------------------
    public static IEnumerable<object[]> IdempotencyCases()
    {
        foreach (var c in LoadAllCases())
            yield return new object[] { c.Id, c.SourceKeys };
    }

    // -----------------------------------------------------------------------
    // Test 1: Core conversion — each corpus case individually.
    // -----------------------------------------------------------------------
    [Theory]
    [MemberData(nameof(AllCorpusCases))]
    public void Convert_CorpusCase_MatchesExpected(
        string id, string sourceKeys, string expected, string description)
    {
        // id and description are used to label test cases in the test runner output.
        _ = id;
        _ = description;
        var actual = _converter.Convert(sourceKeys);
        Assert.Equal(expected, actual);
    }

    // -----------------------------------------------------------------------
    // Test 2: Idempotency — Convert(Convert(x)) == Convert(x)
    // -----------------------------------------------------------------------
    [Theory]
    [MemberData(nameof(IdempotencyCases))]
    public void Convert_Idempotent(string id, string sourceKeys)
    {
        _ = id;
        var once = _converter.Convert(sourceKeys);
        var twice = _converter.Convert(once);
        Assert.Equal(once, twice);
    }

    // -----------------------------------------------------------------------
    // Test 3: URL protection — URL embedded in Bijoy text passes through.
    // -----------------------------------------------------------------------
    [Theory]
    [InlineData("https://example.com")]
    [InlineData("http://www.test.org/path?q=1")]
    [InlineData("www.bbc.co.uk")]
    [InlineData("user@example.com")]
    public void Convert_UrlInInput_PassesThroughUnchanged(string url)
    {
        // Run a bare URL through — it must appear verbatim in the output.
        var result = _converter.Convert(url);
        Assert.Contains(url, result);
    }

    // -----------------------------------------------------------------------
    // Test 4: Already-Unicode text is a no-op.
    // -----------------------------------------------------------------------
    [Theory]
    [InlineData("আমি")]          // ami (I)
    [InlineData("বাংলাদেশ")]      // Bangladesh
    [InlineData("মুক্তি")]        // mukti (freedom)
    [InlineData("")]             // empty
    [InlineData("   ")]          // whitespace only
    public void Convert_AlreadyUnicode_ReturnsUnchanged(string unicodeText)
    {
        var result = _converter.Convert(unicodeText);
        // NFC normalisation is idempotent on already-NFC text, so result equals input.
        Assert.Equal(unicodeText.Normalize(System.Text.NormalizationForm.FormC), result);
    }

    // -----------------------------------------------------------------------
    // Test 5: Null / empty string never throws (D-0008 fuzz-safe guarantee).
    // -----------------------------------------------------------------------
    [Fact]
    public void Convert_EmptyString_ReturnsEmpty()
    {
        Assert.Equal("", _converter.Convert(""));
    }

    [Fact]
    public void IsBijoyText_EmptyString_ReturnsFalse()
    {
        // IsBijoyText treats empty string as not Bijoy — no glyphs present.
        Assert.False(_converter.IsBijoyText(""));
    }

    [Fact]
    public void IsBijoyText_UnicodeText_ReturnsFalse()
    {
        // Already-Unicode text contains no Bijoy source glyphs.
        Assert.False(_converter.IsBijoyText("আমি বাংলা"));
    }
}
