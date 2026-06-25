// GlyphMap.cs — Port of mapping-data.ts
// Reads bijoy-sutonnymj.json and builds the glyph substitution map used by the
// greedy longest-match scanner in Converter.cs.
//
// Guarantees:
//   - Map is populated once at construction time; the object is thereafter immutable.
//   - KeyLengths is sorted longest-first so the Converter can do a single pass.
//   - SourceGlyphs contains every character that can start a source key; used as
//     the idempotency guard (if none are present, conversion is a no-op).

using System.Text.Json;

namespace Mukti.Engine;

/// <summary>
/// Immutable glyph substitution table loaded from a bijoy-sutonnymj.json data file.
/// </summary>
public sealed class GlyphMap
{
    /// <summary>Source-glyph string to Unicode Bengali target string.</summary>
    public IReadOnlyDictionary<string, string> Map { get; }

    /// <summary>Distinct source-key lengths sorted longest-first for greedy matching.</summary>
    public IReadOnlyList<int> KeyLengths { get; }

    /// <summary>Every character that can appear in any source key (idempotency guard).</summary>
    public IReadOnlySet<char> SourceGlyphs { get; }

    /// <summary>Version string from the JSON data file.</summary>
    public string DataVersion { get; }

    /// <summary>
    /// Load and build the glyph map from the given JSON file path.
    /// </summary>
    /// <param name="filePath">Absolute path to bijoy-sutonnymj.json.</param>
    /// <exception cref="InvalidDataException">Thrown if the JSON is malformed or missing required fields.</exception>
    public GlyphMap(string filePath)
    {
        var json = File.ReadAllText(filePath, System.Text.Encoding.UTF8);
        (Map, KeyLengths, SourceGlyphs, DataVersion) = Initialize(json);
    }

    /// <summary>
    /// Build the glyph map directly from JSON content (no file I/O).
    /// Intended for Blazor WASM where the JSON is fetched via HttpClient.
    /// </summary>
    /// <param name="input">The raw JSON content of bijoy-sutonnymj.json.</param>
    /// <param name="isContent">Must be <c>true</c>; guards against accidental file-path mis-use.</param>
    /// <exception cref="ArgumentException">Thrown if <paramref name="isContent"/> is <c>false</c>.</exception>
    /// <exception cref="InvalidDataException">Thrown if the JSON is malformed or missing required fields.</exception>
    public GlyphMap(string input, bool isContent)
    {
        if (!isContent)
            throw new ArgumentException("Pass isContent: true when supplying JSON content directly. Use GlyphMap(string filePath) for file paths.", nameof(isContent));

        (Map, KeyLengths, SourceGlyphs, DataVersion) = Initialize(input);
    }

    /// <summary>
    /// Parses <paramref name="jsonContent"/> and returns the four immutable fields.
    /// Shared by both public constructors so the parsing logic is not duplicated.
    /// </summary>
    private static (IReadOnlyDictionary<string, string> Map,
                    IReadOnlyList<int> KeyLengths,
                    IReadOnlySet<char> SourceGlyphs,
                    string DataVersion) Initialize(string jsonContent)
    {
        using var doc = JsonDocument.Parse(jsonContent);
        var root = doc.RootElement;

        // Read version
        var dataVersion = root.TryGetProperty("version", out var ver)
            ? ver.GetString() ?? "unknown"
            : "unknown";

        // Build the map from the "map" array
        var map = new Dictionary<string, string>();
        var lengthSet = new HashSet<int>();
        var glyphChars = new HashSet<char>();

        if (!root.TryGetProperty("map", out var mapArray))
            throw new InvalidDataException("bijoy-sutonnymj.json is missing the 'map' array.");

        foreach (var entry in mapArray.EnumerateArray())
        {
            if (!entry.TryGetProperty("source", out var sourceProp) ||
                !entry.TryGetProperty("target", out var targetProp))
                continue;

            // Build source string from array of Unicode code points
            var codePoints = new List<int>();
            foreach (var cp in sourceProp.EnumerateArray())
                codePoints.Add(cp.GetInt32());

            if (codePoints.Count == 0) continue;

            var sourceStr = string.Concat(codePoints.Select(cp => char.ConvertFromUtf32(cp)));
            var targetStr = targetProp.GetString() ?? string.Empty;

            // Last-write wins for duplicate keys (mirrors JS Map behaviour)
            map[sourceStr] = targetStr;
            lengthSet.Add(sourceStr.Length);
            foreach (var c in sourceStr)
                glyphChars.Add(c);
        }

        return (map,
                lengthSet.OrderByDescending(l => l).ToList().AsReadOnly(),
                glyphChars,
                dataVersion);
    }
}
