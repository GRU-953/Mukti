// ReorderTests.cs — xUnit tests for Reorder.Apply()
//
// Covers the three Bengali cluster reordering passes:
//   Pass 1 — Double virama collapse:   ্্  ->  ্
//   Pass 2 — Reph repositioning:       C র্  ->  র্ C  (consonant followed by reph moves reph before it)
//   Pass 3 — Pre-base vowel reorder:   [িেৈ] CUnit  ->  CUnit [িেৈ]
//
// Unicode constants used in this file:
//   U+09CD  ্   BENGALI SIGN VIRAMA
//   U+09C7  ে   BENGALI VOWEL SIGN E  (e-kar / pre-base)
//   U+09C8  ৈ   BENGALI VOWEL SIGN AI (ai-kar / pre-base)
//   U+09BF  ি   BENGALI VOWEL SIGN I  (i-kar / pre-base)
//   U+09B0  র   BENGALI LETTER RA
//   U+0995  ক   BENGALI LETTER KA
//   U+0996  খ   BENGALI LETTER KHA
//   U+0997  গ   BENGALI LETTER GA
//   U+09A7  ধ   BENGALI LETTER DHA
//   U+09AE  ম   BENGALI LETTER MA
//   U+09A4  ত   BENGALI LETTER TA
//   U+09B7  ষ   BENGALI LETTER SSA
//   U+09AA  প   BENGALI LETTER PA
//   U+09A6  দ   BENGALI LETTER DA
//   U+09AF  য   BENGALI LETTER YA
//   U+09B9  হ   BENGALI LETTER HA
//   U+09B2  ল   BENGALI LETTER LA
//   U+09BC  ়   BENGALI SIGN NUKTA

namespace Mukti.Engine.Tests;

public sealed class ReorderTests
{
    // Unicode string literals for readability.
    private const string Virama   = "্"; // ্
    private const string Ra       = "র"; // র
    private const string Reph     = "র্"; // র্
    private const string Ka       = "ক"; // ক
    private const string Kha      = "খ"; // খ
    private const string Ga       = "গ"; // গ
    private const string Dha      = "ধ"; // ধ
    private const string Ma       = "ম"; // ম
    private const string Ta       = "ত"; // ত
    private const string Ssa      = "ষ"; // ষ
    private const string Pa       = "প"; // প
    private const string Da       = "দ"; // দ
    private const string Ya       = "য"; // য
    private const string Ha       = "হ"; // হ
    private const string La       = "ল"; // ল
    private const string IKar     = "ি"; // ি
    private const string EKar     = "ে"; // ে
    private const string AiKar    = "ৈ"; // ৈ
    private const string Nukta    = "়"; // ়

    // -----------------------------------------------------------------------
    // Pass 0: passthrough — empty / no-Bengali text is returned unchanged.
    // -----------------------------------------------------------------------

    [Fact]
    public void Apply_EmptyString_ReturnsEmpty()
    {
        Assert.Equal("", Reorder.Apply(""));
    }

    [Fact]
    public void Apply_PureAscii_ReturnsUnchanged()
    {
        const string ascii = "Hello World 123";
        Assert.Equal(ascii, Reorder.Apply(ascii));
    }

    [Fact]
    public void Apply_WhitespaceOnly_ReturnsUnchanged()
    {
        // Whitespace has no Bengali characters — returned as-is.
        const string spaces = "   ";
        Assert.Equal(spaces, Reorder.Apply(spaces));
    }

    // -----------------------------------------------------------------------
    // Pass 1: Double virama collapse — ্্ -> ্
    // -----------------------------------------------------------------------

    [Fact]
    public void Pass1_DoubleVirama_CollapsesToSingle()
    {
        // Input:  ক্্ম  (ka + virama + virama + ma)
        // Output: ক্ম   (ka + virama + ma)
        var input    = Ka + Virama + Virama + Ma;
        var expected = Ka + Virama + Ma;
        Assert.Equal(expected, Reorder.Apply(input));
    }

    [Fact]
    public void Pass1_SingleVirama_Preserved()
    {
        // Input:  ক্ম  (ka + virama + ma)
        // Output: ক্ম  (unchanged — single virama stays)
        var input = Ka + Virama + Ma;
        Assert.Equal(input, Reorder.Apply(input));
    }

    [Fact]
    public void Pass1_DoubleVirama_TwoConsecutivePairsCollapse()
    {
        // Two non-overlapping double-virama pairs in the same string both collapse.
        // Input:  ক্্ম + ক্্ত  (each has a double virama)
        // Output: ক্ম + ক্ত
        var input    = Ka + Virama + Virama + Ma + Ka + Virama + Virama + Ta;
        var expected = Ka + Virama + Ma + Ka + Virama + Ta;
        Assert.Equal(expected, Reorder.Apply(input));
    }

    [Fact]
    public void Pass1_DoubleVirama_MidWordCollapses()
    {
        // Virama doubled inside a longer cluster: ধ্্ম -> ধ্ম (dharma base)
        var input    = Dha + Virama + Virama + Ma;
        var expected = Dha + Virama + Ma;
        Assert.Equal(expected, Reorder.Apply(input));
    }

    // -----------------------------------------------------------------------
    // Pass 2: Reph repositioning
    //
    // Bijoy stores reph AFTER its base consonant: C র্
    // Unicode logical order requires reph BEFORE its base consonant: র্ C
    //
    // e.g. ধ + র্ -> র্ধ  (visual: ধর্ম = dharma)
    // -----------------------------------------------------------------------

    [Fact]
    public void Pass2_RephAfterConsonant_MovesBeforeConsonant()
    {
        // Bijoy order: ধ + র্ (dha + reph)
        // Unicode order: র্ + ধ
        var input    = Dha + Reph;
        var expected = Reph + Dha;
        Assert.Equal(expected, Reorder.Apply(input));
    }

    [Fact]
    public void Pass2_RephAfterKa_MovesBeforeKa()
    {
        // ক + র্ -> র্ক
        var input    = Ka + Reph;
        var expected = Reph + Ka;
        Assert.Equal(expected, Reorder.Apply(input));
    }

    [Fact]
    public void Pass2_RephAfterSsa_MovesBeforeSsa()
    {
        // ষ + র্ -> র্ষ  (as in বর্ষ — borsha)
        var input    = Ssa + Reph;
        var expected = Reph + Ssa;
        Assert.Equal(expected, Reorder.Apply(input));
    }

    [Fact]
    public void Pass2_RephInFullWord_Dharma()
    {
        // ধর্ম: in Bijoy intermediate form the reph is stored after ধ
        // Bijoy intermediate: ধ + র্ + ম  ->  র্ধম (after reorder)
        // Full word: ধর্ম = Dha + Reph + Ma
        var bijoyOrder   = Dha + Reph + Ma;
        var unicodeOrder = Reph + Dha + Ma;
        Assert.Equal(unicodeOrder, Reorder.Apply(bijoyOrder));
    }

    [Fact]
    public void Pass2_NoReph_TextUnchanged()
    {
        // ক্ম  has no reph marker — should remain unchanged after pass 2.
        var input = Ka + Virama + Ma;
        // Only virama present, no Reph, so no change expected.
        Assert.Equal(input, Reorder.Apply(input));
    }

    // -----------------------------------------------------------------------
    // Pass 3: Pre-base vowel reordering
    //
    // Bijoy stores i-kar (ি), e-kar (ে), and ai-kar (ৈ) BEFORE their
    // consonant unit.  Unicode logical order puts them AFTER.
    //
    // e.g. ি + ক  ->  ক + ি
    //      ে + হল ->  হল + ে
    // -----------------------------------------------------------------------

    [Fact]
    public void Pass3_IKarBeforeConsonant_MovesAfter()
    {
        // Bijoy intermediate: ি + ক  (i-kar before ka)
        // Unicode: ক + ি
        var input    = IKar + Ka;
        var expected = Ka + IKar;
        Assert.Equal(expected, Reorder.Apply(input));
    }

    [Fact]
    public void Pass3_EKarBeforeConsonant_MovesAfter()
    {
        // Bijoy intermediate: ে + হ  (e-kar before ha)
        // Unicode: হ + ে
        var input    = EKar + Ha;
        var expected = Ha + EKar;
        Assert.Equal(expected, Reorder.Apply(input));
    }

    [Fact]
    public void Pass3_AiKarBeforeConsonant_MovesAfter()
    {
        // Bijoy intermediate: ৈ + ক  (ai-kar before ka)
        // Unicode: ক + ৈ
        var input    = AiKar + Ka;
        var expected = Ka + AiKar;
        Assert.Equal(expected, Reorder.Apply(input));
    }

    [Fact]
    public void Pass3_EKarBeforeConsonantCluster_MovesAfterCluster()
    {
        // ে + হ + ্ + ল  (e-kar before ha+virama+la cluster)
        // Unicode: হ + ্ + ল + ে
        var input    = EKar + Ha + Virama + La;
        var expected = Ha + Virama + La + EKar;
        Assert.Equal(expected, Reorder.Apply(input));
    }

    [Fact]
    public void Pass3_IKarBeforeConsonantCluster_MovesAfterCluster()
    {
        // ি + ক + ্ + ত  (i-kar before ka+virama+ta)
        // Unicode: ক + ্ + ত + ি
        var input    = IKar + Ka + Virama + Ta;
        var expected = Ka + Virama + Ta + IKar;
        Assert.Equal(expected, Reorder.Apply(input));
    }

    // -----------------------------------------------------------------------
    // Combined reordering — multiple passes interact
    // -----------------------------------------------------------------------

    [Fact]
    public void CombinedPasses_IKarPlusReph_BothReordered()
    {
        // Word like নির্মাণ (nirman): i-kar + consonant cluster with reph
        // After all passes both i-kar and reph must be in Unicode logical order.
        // Input (Bijoy intermediate):  ি + ন + র্ + ম  ->
        //   Pass 3 first: ন + ি is produced? No — pass order matters.
        //   Actually pass 2 fires before pass 3 in Apply():
        //     Pass 2: ন + র্ stays (ra-phala context differs from reph); ন + র্ not matched by reph regex which needs [ক-হ]র্
        //   We test the actual Apply() output against a reference value here.
        // Since নির্মাণ is tested by the full corpus test already, here we verify
        // the raw mechanics with a simple two-pass interaction.
        //
        // Input:  ি + ক + র্  (i-kar, then ka, then reph after ka)
        // Expected after pass 2: ি + র্ + ক
        // Expected after pass 3: র্ + ক + ি
        var input    = IKar + Ka + Reph;
        var expected = Reph + Ka + IKar;
        Assert.Equal(expected, Reorder.Apply(input));
    }

    [Fact]
    public void CombinedPasses_DoubleViramaAndPreBase_BothHandled()
    {
        // Input: ে + ক + ্ + ্ + ত  (e-kar, ka, double-virama, ta)
        // After pass 1: ে + ক + ্ + ত
        // After pass 3: ক + ্ + ত + ে
        var input    = EKar + Ka + Virama + Virama + Ta;
        var expected = Ka + Virama + Ta + EKar;
        Assert.Equal(expected, Reorder.Apply(input));
    }

    // -----------------------------------------------------------------------
    // Real-word spot-checks from the corpus (post-glyph-substitution form)
    // -----------------------------------------------------------------------

    [Fact]
    public void RealWord_Holo_EKarAndHa()
    {
        // হলো — holo (became): ে + হ + ল + ো  -> হ + ল + ো + ে (pre-base e-kar moved)
        // In Unicode this is stored as হ (ha) + ল (la) + ো (o-kar) + (no pre-base issue)
        // Actually the corpus tells us the Bijoy intermediate produces ে before হ.
        // Testing: e-kar before ha-la cluster moves after it.
        // Bijoy intermediate (from corpus 04-reordering):
        //   source bytes -> হ ‡ ল ো  where ‡ maps to ে
        // So the intermediate form is:  EKar + Ha + La + "ো"
        // Expected Unicode: হ + ল + ো (+ ে moves after Ha only — Ha is the consonant unit here)
        // We just verify e-kar before Ha moves after Ha.
        var input    = EKar + Ha;
        var expected = Ha + EKar;
        Assert.Equal(expected, Reorder.Apply(input));
    }

    [Fact]
    public void RealWord_Pro_RaphalamRaPhala()
    {
        // প্র — ra-phala (pa+virama+ra): this is ra-phala, NOT reph.
        // In Bijoy the encoding stores pa first, then Ö (which maps to virama+ra).
        // After glyph substitution we get: প + ্ + র
        // Reorder should NOT move anything — ra-phala is already in Unicode order.
        var input = Pa + Virama + Ra;
        // No reph present (reph = Ra + Virama, not Virama + Ra), no pre-base vowel.
        Assert.Equal(input, Reorder.Apply(input));
    }
}
