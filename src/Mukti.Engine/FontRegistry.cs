// FontRegistry.cs — Port of font-registry.ts
// Font classification policy: exact-match against curated known lists, with a
// fallback "looks Bangla-legacy" heuristic that surfaces unknown fonts as
// `Unsupported` rather than silently ignoring them.
//
// Normalization: trim + lowercase + collapse internal whitespace to a single space.
//
// Deliberately NO fuzzy "MJ-suffix" matching (do-not-repeat H4, finding 02 —
// NikoshMJ is a Unicode font and was a confirmed false positive in the prior code).
//
// Pure logic; no I/O, no Office.js, no network.

using System.Text.RegularExpressions;

namespace Mukti.Engine;

/// <summary>
/// Classification of a font by its legacy-Bijoy vs. Unicode-Bengali status.
/// </summary>
public enum FontClass
{
    /// <summary>Known Bijoy/SutonnyMJ legacy encoding font — should be converted.</summary>
    Bijoy,
    /// <summary>Known Unicode Bengali font — leave untouched.</summary>
    Unicode,
    /// <summary>Looks like a legacy Bangla font but is not on the known list; warn and leave untouched.</summary>
    Unsupported,
    /// <summary>Not a Bengali font; ignore.</summary>
    NonBengali,
}

/// <summary>
/// Result of classifying a font name.
/// </summary>
/// <param name="FontName">The original font name as supplied.</param>
/// <param name="Class">The classification result.</param>
/// <param name="Reason">Human-readable reason, populated only for <see cref="FontClass.Unsupported"/>.</param>
public sealed record FontClassification(string FontName, FontClass Class, string? Reason = null);

/// <summary>
/// Classifies font names as Bijoy, Unicode, Unsupported, or NonBengali.
/// </summary>
public sealed partial class FontRegistry
{
    // -----------------------------------------------------------------------
    // Curated known Bijoy/SutonnyMJ-family font names (normalized).
    // Starter set per finding 02-fonts.md (REUSE verdict ADAPT): core SutonnyMJ
    // variants plus a representative set of common Ananda Computers Bijoy family
    // names. Deliberately scrubbed of miscategorised and over-generic entries
    // (no bare "bangla"/"bijoy", no fuzzy matching).
    // -----------------------------------------------------------------------
    private static readonly IReadOnlyList<string> _knownBijoyFonts = new[]
    {
        "sutonnymj",
        "sutonnymj bold",
        "sutonnymj italic",
        "sutonny mj",
        "sutonnycmj",
        "sutonnyemj",
        "sutonnysushreemj",
        "tonnybanglaj",
        // common Ananda Computers river-named Bijoy fonts
        "gangamj",
        "padmamj",
        "jomunamj",
        "meghnamj",
        "teeshtamj",
        "turagmj",
        "sandipanmj",
        // common newspaper Bijoy fonts
        "jugantormj",
        "samakalmj",
        "jaijaidinmj",
    };

    // -----------------------------------------------------------------------
    // Known Unicode Bengali fonts (left untouched by the converter).
    // -----------------------------------------------------------------------
    private static readonly IReadOnlyList<string> KnownUnicodeFonts = new[]
    {
        "solaimanlipi",
        "noto sans bengali",
        "noto serif bengali",
        "kohinoor bangla",
        "nikosh",
        "nikoshban",
        "kalpurush",
        // SutonnyOMJ is a Unicode-OpenType font despite the MJ-like name (finding 02).
        "sutonnyomj",
        // Shonar Bangla ships with Windows — Unicode, not Bijoy.
        "shonar bangla",
    };

    // -----------------------------------------------------------------------
    // Substrings that flag a name as "Bangla-looking" so an unknown match is
    // surfaced as `Unsupported` rather than silently ignored.  Lowercase.
    // -----------------------------------------------------------------------
    private static readonly IReadOnlyList<string> BengaliLikeMarkers = new[]
    {
        "mj",
        "bangla",
        "bengali",
        "lipi",
        "bijoy",
    };

    private static readonly HashSet<string> _bijoySet =
        new(_knownBijoyFonts, StringComparer.Ordinal);

    private static readonly HashSet<string> _unicodeSet =
        new(KnownUnicodeFonts, StringComparer.Ordinal);

    // Pre-compiled whitespace collapser used during normalization.
    [GeneratedRegex(@"\s+", RegexOptions.Compiled)]
    private static partial Regex WhitespaceRegex();

    private const string UnsupportedReason =
        "Looks like a legacy Bangla font but is not on Mukti's known list; not converted.";

    /// <summary>
    /// Classify a font by name.
    /// </summary>
    /// <param name="fontName">Font name as it appears in the document.</param>
    /// <returns>A <see cref="FontClassification"/> describing the font's status.</returns>
    public FontClassification Classify(string fontName)
    {
        var norm = Normalize(fontName);

        if (_unicodeSet.Contains(norm))
            return new FontClassification(fontName, FontClass.Unicode);

        if (_bijoySet.Contains(norm))
            return new FontClassification(fontName, FontClass.Bijoy);

        // Not on either known list: is it Bangla-looking?
        foreach (var marker in BengaliLikeMarkers)
        {
            if (norm.Contains(marker, StringComparison.Ordinal))
                return new FontClassification(fontName, FontClass.Unsupported, UnsupportedReason);
        }

        return new FontClassification(fontName, FontClass.NonBengali);
    }

    /// <summary>Trim, lowercase, and collapse internal whitespace to a single space.</summary>
    private static string Normalize(string name) =>
        WhitespaceRegex().Replace(name.Trim().ToLowerInvariant(), " ");

    // -----------------------------------------------------------------------
    // Public read-only accessors for the curated lists (mirrors FontRegistry
    // interface in contracts.ts).
    // -----------------------------------------------------------------------

    /// <summary>Known Bijoy/SutonnyMJ font names (normalized).</summary>
    public IReadOnlyList<string> KnownBijoy => _knownBijoyFonts;

    /// <summary>
    /// Known Bijoy/SutonnyMJ font names (normalized).
    /// Alias for <see cref="KnownBijoy"/>; used by the Blazor WASM host to pass
    /// font names to the Office.js JavaScript layer.
    /// </summary>
    public IReadOnlyList<string> KnownBijoyFonts => _knownBijoyFonts;

    /// <summary>Known Unicode Bengali font names (normalized).</summary>
    public IReadOnlyList<string> KnownUnicode => KnownUnicodeFonts;

    /// <summary>Substrings that flag a name as possibly legacy Bangla.</summary>
    public IReadOnlyList<string> BengaliMarkers => BengaliLikeMarkers;
}
