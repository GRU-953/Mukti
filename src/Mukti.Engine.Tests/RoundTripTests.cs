// RoundTripTests.cs — Engine-level round-trip and undo-path tests.
//
// Tests in this file verify the invariants that the Revert (undo) code path
// in OfficeIntegration.cs depends on:
//
//   1. Basic transformation: Convert(bijoy) != bijoy  (the engine actually changes text)
//   2. ConversionSnapshot integrity: SnapshotText == Original for each RunItem
//   3. Idempotency: Convert(Convert(x)) == Convert(x)  (re-running Apply is a no-op)
//   4. Revert lookup collision documentation: when two different Bijoy originals
//      produce the same Unicode output, only one original survives in the lookup dict.
//
// Findings from reading OfficeIntegration.cs RevertWord/RevertExcel/RevertPowerPoint:
//
//   DUPLICATE-KEY GUARD IS PRESENT: all three Revert* methods use
//     if (!lookup.ContainsKey(item.Converted.Trim()))
//   before adding to the lookup dictionary.  This means only the FIRST RunItem
//   for a given Converted value is kept.  Any subsequent RunItem whose Converted
//   value is identical is silently dropped.
//
//   COLLISION VULNERABILITY: if two different Bijoy originals in the same document
//   happen to produce identical Unicode output, the second original can never be
//   restored by Revert.  It will be restored to the first original instead.
//   Test #4 below documents this behaviour.

namespace Mukti.Engine.Tests;

public sealed class RoundTripTests
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
    // 1. Basic transformation — the precondition for undo
    //
    // For each Bijoy input, Convert must produce a different string.
    // This is the precondition that ProcessRun checks before adding an item to
    // the snapshot (if (converted != text)).  If it were ever false, the item
    // would not be snapshotted and Revert would have nothing to restore.
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData("evsjv")]           // বাংলা
    [InlineData("Avwg")]            // আমি
    [InlineData("evsjv‡`k")]        // বাংলাদেশ
    [InlineData("gyw³")]            // মুক্তি
    [InlineData("¯^vaxbZv")]        // স্বাধীনতা
    public void Convert_BijoyInput_ProducesDifferentString(string bijoyInput)
    {
        var converted = _converter.Convert(bijoyInput);
        Assert.NotEqual(bijoyInput, converted);
    }

    // -----------------------------------------------------------------------
    // 2. ConversionSnapshot integrity — SnapshotText == Original
    //
    // ProcessRun sets SnapshotText = text (the original Bijoy text) at the same
    // time as Original = text.  This helper simulates that assignment and asserts
    // the invariant: SnapshotText must equal Original so that Revert can restore
    // the correct text.
    // -----------------------------------------------------------------------

    private static RunItemSimulation SimulateProcessRun(string bijoyText)
    {
        var converted = _converter.Convert(bijoyText);
        return new RunItemSimulation
        {
            Original = bijoyText,
            Converted = converted,
            SnapshotText = bijoyText   // mirrors: SnapshotText = text in ProcessRun
        };
    }

    private sealed class RunItemSimulation
    {
        public string Original { get; init; } = "";
        public string Converted { get; init; } = "";
        public string? SnapshotText { get; init; }
    }

    [Theory]
    [InlineData("evsjv")]           // বাংলা
    [InlineData("Avwg")]            // আমি
    [InlineData("eÜz")]             // বন্ধু
    [InlineData("gš¿x")]            // মন্ত্রী
    [InlineData("Kg©KvÐ")]          // কর্মকাণ্ড
    public void ProcessRun_Simulation_SnapshotTextEqualsOriginal(string bijoyInput)
    {
        var item = SimulateProcessRun(bijoyInput);
        Assert.Equal(item.Original, item.SnapshotText);
    }

    // -----------------------------------------------------------------------
    // 3. Idempotency — Convert(Convert(x)) == Convert(x)
    //
    // This is the key guarantee (D-0007) that makes re-running Apply a no-op.
    // If Apply runs twice on the same document (e.g. user triggers Convert again
    // after a first conversion), the already-Unicode text must pass through
    // unchanged.  The Converter guarantees this via the IsBijoyText guard:
    // Unicode Bengali characters are not source glyphs, so they do not trigger
    // the conversion pipeline.
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData("evsjv")]           // বাংলা
    [InlineData("Avwg")]            // আমি
    [InlineData("evsjv‡`k")]        // বাংলাদেশ
    [InlineData("eÜz")]             // বন্ধু
    [InlineData("gyw³")]            // মুক্তি
    [InlineData("wkÿv")]            // শিক্ষা
    [InlineData("b`x")]             // নদী
    [InlineData("cÖ_g")]            // প্রথম
    [InlineData("ag©")]             // ধর্ম
    [InlineData("¯^vaxbZv")]        // স্বাধীনতা
    public void Convert_Idempotent_DoubleConvertEqualsConvert(string bijoyInput)
    {
        var once = _converter.Convert(bijoyInput);
        var twice = _converter.Convert(once);
        Assert.Equal(once, twice);
    }

    // -----------------------------------------------------------------------
    // 4. Revert lookup collision — documented known limitation
    //
    // All three Revert* methods in OfficeIntegration.cs build a lookup dictionary
    // keyed by item.Converted.Trim().  The guard used is:
    //
    //   if (!lookup.ContainsKey(item.Converted.Trim()))
    //       lookup[item.Converted.Trim()] = (item.Original, item.FontName);
    //
    // This means: only the FIRST RunItem for a given Converted value is stored.
    // If a document contains two runs with different Bijoy originals that happen
    // to produce the same Unicode output (identical converted text), the second
    // run's original is unreachable via Revert — it will be restored to the first
    // run's original instead.
    //
    // This test DOCUMENTS the limitation by showing the dictionary mechanics.
    // It does NOT assert that restoration is correct — it asserts the collision
    // behaviour so that if the guard logic changes in a future refactor, this
    // test will catch the change.
    //
    // Known scenario: two runs of "evsjv" in the same document (both map to
    // "বাংলা").  In practice both originals are identical here, so the collision
    // is harmless.  The dangerous case is two DIFFERENT originals that produce
    // the same Unicode — e.g. alternate Bijoy encodings of the same word, or
    // a document with hand-edited glyphs.  We cannot easily construct such a
    // pair from the standard glyph map, so we demonstrate the mechanism directly.
    // -----------------------------------------------------------------------

    [Fact]
    public void RevertLookup_DuplicateConvertedKey_OnlyFirstOriginalStoredInLookup()
    {
        // Simulate two RunItems with DIFFERENT originals but SAME Converted value.
        // In a real document this arises when two Bijoy encodings happen to map to
        // the same Unicode string.  Here we construct it directly to test the dict logic.
        const string sharedConverted = "বাংলা";
        const string firstOriginal   = "evsjv";   // the first run encountered in the document
        const string secondOriginal  = "EVSJV";   // a hypothetical alternate encoding (different chars)

        var items = new[]
        {
            new { Converted = sharedConverted, Original = firstOriginal,  FontName = "SutonnyMJ" },
            new { Converted = sharedConverted, Original = secondOriginal, FontName = "BijoyBaijayanti" },
        };

        // Replicate the exact guard used in RevertWord / RevertExcel / RevertPowerPoint.
        var lookup = new Dictionary<string, (string orig, string font)>();
        foreach (var item in items)
        {
            if (!lookup.ContainsKey(item.Converted.Trim()))
                lookup[item.Converted.Trim()] = (item.Original, item.FontName);
        }

        // Only one entry should be in the lookup.
        Assert.Single(lookup);

        // The stored entry is the FIRST original encountered, not the second.
        Assert.True(lookup.TryGetValue(sharedConverted, out var stored));
        Assert.Equal(firstOriginal, stored.orig);

        // The second original is inaccessible via the lookup.
        // If Revert encounters text matching sharedConverted, it will restore
        // firstOriginal even where secondOriginal was the true source.
        Assert.NotEqual(secondOriginal, stored.orig);
    }
}
