using Mukti.Engine;
using System.Collections.Generic;

namespace Mukti.WindowsAddin;

// A single text run found during scanning
public class RunItem
{
    public string Original { get; set; } = "";
    public string Converted { get; set; } = "";
    public string FontName { get; set; } = "";
    public int ParagraphIndex { get; set; }
    public int RunIndex { get; set; }
    public string AppType { get; set; } = ""; // "Word", "Excel", "PowerPoint"
    // Stored original text for revert
    public string? SnapshotText { get; set; }
}

// Result of a scan operation
public class ConversionSnapshot
{
    public List<RunItem> Items { get; set; } = new();
    public List<string> UnsupportedFonts { get; set; } = new();
    public string AppType { get; set; } = "";
}

// Handles all three Office apps via dynamic
public class OfficeIntegration
{
    private readonly dynamic _app;
    private readonly Converter _converter;
    private readonly FontRegistry _fontRegistry;

    public OfficeIntegration(dynamic app)
    {
        _app = app;
        _converter = Connect.GetConverter();
        _fontRegistry = Connect.GetFontRegistry();
    }

    // ── Detect app type ───────────────────────────────────────────────────
    private string GetAppType()
    {
        try
        {
            var name = (string)_app.Name;
            if (name.Contains("Word")) return "Word";
            if (name.Contains("Excel")) return "Excel";
            if (name.Contains("PowerPoint")) return "PowerPoint";
        }
        catch { }
        return "Unknown";
    }

    // ── Scan ─────────────────────────────────────────────────────────────
    public ConversionSnapshot Scan()
    {
        var appType = GetAppType();
        return appType switch
        {
            "Word"        => ScanWord(),
            "Excel"       => ScanExcel(),
            "PowerPoint"  => ScanPowerPoint(),
            _             => new ConversionSnapshot { AppType = appType }
        };
    }

    private ConversionSnapshot ScanWord()
    {
        var snap = new ConversionSnapshot { AppType = "Word" };
        try
        {
            var doc = _app.ActiveDocument;
            int paraIdx = 0;
            foreach (dynamic para in doc.Paragraphs)
            {
                ScanRange(para.Range, snap, paraIdx++);
            }
        }
        catch (Exception ex)
        {
            // Log but don't crash
            snap.UnsupportedFonts.Add($"Scan error: {ex.Message}");
        }
        return snap;
    }

    private ConversionSnapshot ScanExcel()
    {
        var snap = new ConversionSnapshot { AppType = "Excel" };
        try
        {
            var sheet = _app.ActiveSheet;
            var usedRange = sheet.UsedRange;
            int idx = 0;
            foreach (dynamic cell in usedRange.Cells)
            {
                try
                {
                    string text = (string)cell.Text;
                    if (string.IsNullOrEmpty(text)) continue;
                    string fontName = (string)cell.Font.Name;
                    ProcessRun(text, fontName, snap, idx++, 0);
                }
                catch { }
            }
        }
        catch (Exception ex)
        {
            snap.UnsupportedFonts.Add($"Scan error: {ex.Message}");
        }
        return snap;
    }

    private ConversionSnapshot ScanPowerPoint()
    {
        var snap = new ConversionSnapshot { AppType = "PowerPoint" };
        try
        {
            var pres = _app.ActivePresentation;
            int idx = 0;
            foreach (dynamic slide in pres.Slides)
            {
                foreach (dynamic shape in slide.Shapes)
                {
                    try
                    {
                        if (!shape.HasTextFrame) continue;
                        foreach (dynamic para in shape.TextFrame.TextRange.Paragraphs())
                        {
                            foreach (dynamic run in para.Runs())
                            {
                                string text = (string)run.Text;
                                string fontName = (string)run.Font.Name;
                                ProcessRun(text, fontName, snap, idx++, 0);
                            }
                        }
                    }
                    catch { }
                }
            }
        }
        catch (Exception ex)
        {
            snap.UnsupportedFonts.Add($"Scan error: {ex.Message}");
        }
        return snap;
    }

    private void ScanRange(dynamic range, ConversionSnapshot snap, int paraIdx)
    {
        try
        {
            // Iterate character runs within the range
            int runIdx = 0;
            // Use Words collection for Word documents as a proxy for runs
            foreach (dynamic word in range.Words)
            {
                try
                {
                    string text = (string)word.Text;
                    string fontName = (string)word.Font.Name;
                    if (string.IsNullOrWhiteSpace(text)) continue;
                    ProcessRun(text.Trim(), fontName, snap, paraIdx, runIdx++);
                }
                catch { }
            }
        }
        catch { }
    }

    private void ProcessRun(string text, string fontName, ConversionSnapshot snap, int paraIdx, int runIdx)
    {
        var classification = _fontRegistry.Classify(fontName);
        switch (classification.Class)
        {
            case FontClass.Bijoy:
                var converted = _converter.Convert(text);
                if (converted != text)
                {
                    snap.Items.Add(new RunItem
                    {
                        Original = text,
                        Converted = converted,
                        FontName = fontName,
                        ParagraphIndex = paraIdx,
                        RunIndex = runIdx,
                        AppType = snap.AppType,
                        SnapshotText = text
                    });
                }
                break;
            case FontClass.Unsupported:
                if (!snap.UnsupportedFonts.Contains(fontName))
                    snap.UnsupportedFonts.Add(fontName);
                break;
        }
    }

    // ── Apply ─────────────────────────────────────────────────────────────
    public void Apply(ConversionSnapshot snapshot)
    {
        // For the apply, we do a fresh scan-and-replace in the document.
        // This is safer than trying to navigate by index (which can be invalidated).
        var appType = GetAppType();
        switch (appType)
        {
            case "Word":       ApplyWord(snapshot); break;
            case "Excel":      ApplyExcel(snapshot); break;
            case "PowerPoint": ApplyPowerPoint(snapshot); break;
        }
    }

    private void ApplyWord(ConversionSnapshot snapshot)
    {
        var doc = _app.ActiveDocument;
        // Use Find/Replace approach: scan words and replace Bijoy text
        foreach (dynamic para in doc.Paragraphs)
        {
            foreach (dynamic word in para.Range.Words)
            {
                try
                {
                    string text = (string)word.Text;
                    string fontName = (string)word.Font.Name;
                    var cls = _fontRegistry.Classify(fontName);
                    if (cls.Class != FontClass.Bijoy) continue;
                    var converted = _converter.Convert(text.Trim());
                    if (converted == text.Trim()) continue;
                    // Replace the text in-place
                    word.Text = text.Replace(text.Trim(), converted);
                    word.Font.Name = "Noto Sans Bengali";
                }
                catch { }
            }
        }
    }

    private void ApplyExcel(ConversionSnapshot snapshot)
    {
        var sheet = _app.ActiveSheet;
        foreach (dynamic cell in sheet.UsedRange.Cells)
        {
            try
            {
                string text = (string)cell.Text;
                string fontName = (string)cell.Font.Name;
                var cls = _fontRegistry.Classify(fontName);
                if (cls.Class != FontClass.Bijoy) continue;
                var converted = _converter.Convert(text);
                if (converted == text) continue;
                cell.Value = converted;
                cell.Font.Name = "Noto Sans Bengali";
            }
            catch { }
        }
    }

    private void ApplyPowerPoint(ConversionSnapshot snapshot)
    {
        var pres = _app.ActivePresentation;
        foreach (dynamic slide in pres.Slides)
        {
            foreach (dynamic shape in slide.Shapes)
            {
                try
                {
                    if (!shape.HasTextFrame) continue;
                    foreach (dynamic para in shape.TextFrame.TextRange.Paragraphs())
                    {
                        foreach (dynamic run in para.Runs())
                        {
                            try
                            {
                                string text = (string)run.Text;
                                string fontName = (string)run.Font.Name;
                                var cls = _fontRegistry.Classify(fontName);
                                if (cls.Class != FontClass.Bijoy) continue;
                                var converted = _converter.Convert(text);
                                if (converted == text) continue;
                                run.Text = converted;
                                run.Font.Name = "Noto Sans Bengali";
                            }
                            catch { }
                        }
                    }
                }
                catch { }
            }
        }
    }

    // ── Revert ─────────────────────────────────────────────────────────────
    public void Revert(ConversionSnapshot snapshot)
    {
        // Re-scan the document. Anywhere we find the converted text, put back the original.
        // This is a best-effort revert using the snapshot data.
        var appType = GetAppType();
        switch (appType)
        {
            case "Word": RevertWord(snapshot); break;
        }
    }

    private void RevertWord(ConversionSnapshot snapshot)
    {
        var doc = _app.ActiveDocument;
        // Build lookup: converted -> (original, fontName)
        var lookup = new Dictionary<string, (string orig, string font)>();
        foreach (var item in snapshot.Items)
        {
            if (!lookup.ContainsKey(item.Converted.Trim()))
                lookup[item.Converted.Trim()] = (item.Original, item.FontName);
        }

        foreach (dynamic para in doc.Paragraphs)
        {
            foreach (dynamic word in para.Range.Words)
            {
                try
                {
                    string text = ((string)word.Text).Trim();
                    if (lookup.TryGetValue(text, out var original))
                    {
                        word.Text = ((string)word.Text).Replace(text, original.orig);
                        word.Font.Name = original.font;
                    }
                }
                catch { }
            }
        }
    }
}
