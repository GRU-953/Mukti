// Converter.cs — Port of convert.ts + bijoy_unicode.py
// The main Bijoy/SutonnyMJ -> Unicode Bengali conversion pipeline.
//
// Pipeline (faithful port of the proven Spike-B reference converter + Python reference):
//   idempotency guard  ->  protect URLs/emails  ->  PRE_MAP substitution  ->
//   greedy longest-match glyph substitution  ->  Reorder.Apply  ->
//   POST_MAP cleanup  ->  NFC normalisation.
//
// Guarantees baked into the contract (mirrors contracts.ts, D-0007, D-0008):
//   - Output is always NFC.
//   - Idempotent: Convert(Convert(x)) == Convert(x); and Convert(x) == x for
//     any x that contains no Bijoy source glyphs (already-Unicode is a no-op).
//   - Whitespace preserved verbatim (no tidying).
//   - URLs / emails pass through untouched.
//   - Pure and total: never throws on any valid string input (fuzz-safe).
//     No I/O performed after construction (GlyphMap is fully loaded at ctor time).

using System.Text;
using System.Text.RegularExpressions;

namespace Mukti.Engine;

/// <summary>
/// Converts Bijoy/SutonnyMJ-encoded Bengali text to NFC Unicode.
/// </summary>
public sealed partial class Converter
{
    private readonly GlyphMap _glyphMap;

    // URL / email protection pattern — identical in intent to the TypeScript engine.
    // Captured groups (odd indices after Split) are verbatim protected runs.
    // The pattern matches:
    //   - Scheme-based URLs:  scheme://...
    //   - www. URLs
    //   - Email addresses
    // These ASCII runs must NOT be processed through the Bijoy substitution table.
    [GeneratedRegex(
        @"([A-Za-z][A-Za-z0-9+.\-]*://\S+|www\.\S+|[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,})",
        RegexOptions.Compiled)]
    private static partial Regex ProtectRegex();

    /// <summary>
    /// Create a converter backed by the given pre-loaded glyph map.
    /// </summary>
    /// <param name="glyphMap">A fully loaded <see cref="GlyphMap"/> instance.</param>
    public Converter(GlyphMap glyphMap)
    {
        _glyphMap = glyphMap ?? throw new ArgumentNullException(nameof(glyphMap));
    }

    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="input"/> contains at least
    /// one Bijoy source glyph character.  Used as the idempotency guard (D-0007).
    /// </summary>
    public bool IsBijoyText(string input)
    {
        if (string.IsNullOrEmpty(input)) return false;
        foreach (var c in input)
        {
            if (_glyphMap.SourceGlyphs.Contains(c))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Convert a Bijoy/SutonnyMJ string to NFC Unicode Bengali.
    /// </summary>
    /// <param name="input">Input string; may be empty, already-Unicode, or Bijoy-encoded.</param>
    /// <returns>NFC-normalised Unicode Bengali string.</returns>
    public string Convert(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        // Idempotency / no-op guard: nothing to convert without Bijoy glyphs (D-0007).
        if (!IsBijoyText(input))
            return input.Normalize(NormalizationForm.FormC);

        // Protect URLs and email addresses: Split keeps captured groups at odd indices.
        var parts = ProtectRegex().Split(input);
        var sb = new StringBuilder(input.Length * 2);

        for (var k = 0; k < parts.Length; k++)
        {
            var seg = parts[k] ?? string.Empty;
            if (k % 2 == 1)
            {
                // Protected run (URL / email) — pass through verbatim.
                sb.Append(seg);
            }
            else
            {
                // Normal segment: PRE_MAP -> glyph-map substitution -> cluster reordering.
                sb.Append(Reorder.Apply(MapGlyphs(ApplyMap(seg, PreMap))));
            }
        }

        // POST_MAP cleanup followed by NFC normalisation.
        return ApplyMap(sb.ToString(), PostMap).Normalize(NormalizationForm.FormC);
    }

    // -----------------------------------------------------------------------
    // PRE_MAP and POST_MAP (ported from bijoy_unicode.py)
    // -----------------------------------------------------------------------

    // Applied BEFORE glyph substitution on each non-protected segment.
    private static readonly (string Old, string New)[] PreMap =
    [
        ("yy", "y"),
        ("vv", "v"),
        ("­­", "­"),   // soft-hyphen pair -> single soft-hyphen
        ("y&", "y"),
        ("„&", "„"),         // „& -> „
        ("‡u", "u‡"),        // ‡u -> u‡  (e-kar + chandrabindu display order fix)
        ("wu", "uw"),                  // ি + ঁ display order fix
        (" ,", ","),
        (" |", "|"),
    ];

    // Applied AFTER the main conversion loop, before NFC normalisation.
    private static readonly (string Old, string New)[] PostMap =
    [
        ("০ঃ", "০:"), ("১ঃ", "১:"), ("২ঃ", "২:"), ("৩ঃ", "৩:"), ("৪ঃ", "৪:"),
        ("৫ঃ", "৫:"), ("৬ঃ", "৬:"), ("৭ঃ", "৭:"), ("৮ঃ", "৮:"), ("৯ঃ", "৯:"),
        (" ঃ", ":"),
        ("\nঃ", "\n:"),
        ("]ঃ", "]:"),
        ("[ঃ", "[:"),
        ("  ", " "),
        ("অা", "আ"),
        ("্‌্‌", "্‌"),
        ("স্ত্ম", "স্ত"),
        ("ন্ত্ম", "ন্ত"),
    ];

    /// <summary>
    /// Apply a sequence of literal string substitutions in order.
    /// </summary>
    private static string ApplyMap(string text, (string Old, string New)[] map)
    {
        foreach (var (old, @new) in map)
            if (!string.IsNullOrEmpty(old))
                text = text.Replace(old, @new);
        return text;
    }

    // -----------------------------------------------------------------------
    // Private helpers
    // -----------------------------------------------------------------------

    /// <summary>
    /// Greedy longest-match substitution of Bijoy source glyphs to Unicode.
    /// Iterates KeyLengths (longest first) at each position, consuming the
    /// longest match found before advancing.
    /// </summary>
    private string MapGlyphs(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        var sb = new StringBuilder(text.Length * 2);
        var map = _glyphMap.Map;
        var keyLengths = _glyphMap.KeyLengths;
        var i = 0;

        while (i < text.Length)
        {
            var matched = false;
            foreach (var len in keyLengths)
            {
                // Guard: don't over-read past end of string.
                if (i + len > text.Length) continue;

                var slice = text.Substring(i, len);
                if (map.TryGetValue(slice, out var target))
                {
                    sb.Append(target);
                    i += len;
                    matched = true;
                    break;
                }
            }

            if (!matched)
            {
                sb.Append(text[i]);
                i += 1;
            }
        }

        return sb.ToString();
    }
}
