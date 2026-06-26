using Mukti.Engine;
using System.Collections.Generic;

namespace Mukti.WindowsAddin;

internal static class HardwareProfile
{
    internal static readonly long AvailableRamBytes = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;

    // Office automation is single-threaded apartment (STA), so we never parallelise the scan
    // itself — CPU core count would buy nothing here. This flag only gates the periodic GC hint
    // in ProcessRun, which keeps the working set small on 2 GB machines during very large documents.
    internal static bool IsLowMemory => AvailableRamBytes < 1_500_000_000L;
}

public class RunItem
{
    public string Original { get; set; } = "";
    public string Converted { get; set; } = "";
    public string FontName { get; set; } = "";
    public int ParagraphIndex { get; set; }
    public int RunIndex { get; set; }
    public string AppType { get; set; } = "";
    public string? SnapshotText { get; set; }
}

public class ConversionSnapshot
{
    public List<RunItem> Items { get; set; } = new();
    public List<string> UnsupportedFonts { get; set; } = new();
    public string AppType { get; set; } = "";
    public int FormulaSkippedCount { get; set; }  // U-005
    public int AlreadyUnicodeCount { get; set; }
}

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

    // ── Full-document scan ────────────────────────────────────────────────
    public ConversionSnapshot Scan()
    {
        var appType = GetAppType();
        return appType switch
        {
            "Word"        => ScanWord(selectionOnly: false),
            "Excel"       => ScanExcel(selectionOnly: false),
            "PowerPoint"  => ScanPowerPoint(),
            _             => new ConversionSnapshot { AppType = appType }
        };
    }

    // U-011: selection-only scan
    public ConversionSnapshot ScanSelection()
    {
        var appType = GetAppType();
        return appType switch
        {
            "Word"        => ScanWord(selectionOnly: true),
            "Excel"       => ScanExcel(selectionOnly: true),
            "PowerPoint"  => ScanPowerPoint(),   // PPT selection is complex; fall back to full scan
            _             => new ConversionSnapshot { AppType = appType }
        };
    }

    // ── Word scan ─────────────────────────────────────────────────────────

    private ConversionSnapshot ScanWord(bool selectionOnly)
    {
        var snap = new ConversionSnapshot { AppType = "Word" };
        try
        {
            var doc = _app.ActiveDocument;
            int paraIdx = 0;

            if (selectionOnly)
            {
                var sel = _app.Selection;
                // wdNoSelection = 0
                if ((int)sel.Type == 0)
                {
                    snap.UnsupportedFonts.Add("No text selected.");
                    return snap;
                }
                ScanRange(sel.Range, snap, paraIdx++);
            }
            else
            {
                // Body paragraphs
                foreach (dynamic para in doc.Paragraphs)
                    ScanRange(para.Range, snap, paraIdx++);

                // Word tables
                try
                {
                    foreach (dynamic table in doc.Tables)
                    {
                        try
                        {
                            foreach (dynamic row in table.Rows)
                            {
                                try
                                {
                                    foreach (dynamic cell in row.Cells)
                                        try { ScanRange(cell.Range, snap, paraIdx++); } catch { }
                                }
                                catch { }
                            }
                        }
                        catch { }
                    }
                }
                catch { }

                // U-004: Headers and footers
                try
                {
                    foreach (dynamic section in doc.Sections)
                    {
                        foreach (dynamic hf in section.Headers)
                            try { ScanRange(hf.Range, snap, paraIdx++); } catch { }
                        foreach (dynamic hf in section.Footers)
                            try { ScanRange(hf.Range, snap, paraIdx++); } catch { }
                    }
                }
                catch { }

                // U-004: Footnotes and endnotes
                try
                {
                    foreach (dynamic fn in doc.Footnotes)
                        try { ScanRange(fn.Range, snap, paraIdx++); } catch { }
                }
                catch { }
                try
                {
                    foreach (dynamic en in doc.Endnotes)
                        try { ScanRange(en.Range, snap, paraIdx++); } catch { }
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

    private void ScanRange(dynamic range, ConversionSnapshot snap, int paraIdx)
    {
        try
        {
            int runIdx = 0;
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

    // ── Excel scan ────────────────────────────────────────────────────────

    private ConversionSnapshot ScanExcel(bool selectionOnly)
    {
        var snap = new ConversionSnapshot { AppType = "Excel" };
        try
        {
            if (selectionOnly)
            {
                int idx = 0;
                foreach (dynamic cell in _app.Selection.Cells)
                    ScanExcelCell(cell, snap, ref idx);
            }
            else
            {
                int idx = 0;
                try
                {
                    foreach (dynamic sheet in _app.ActiveWorkbook.Worksheets)
                    {
                        try
                        {
                            foreach (dynamic cell in sheet.UsedRange.Cells)
                                ScanExcelCell(cell, snap, ref idx);
                        }
                        catch { }
                    }
                }
                catch
                {
                    foreach (dynamic cell in _app.ActiveSheet.UsedRange.Cells)
                        ScanExcelCell(cell, snap, ref idx);
                }
            }
        }
        catch (Exception ex)
        {
            snap.UnsupportedFonts.Add($"Scan error: {ex.Message}");
        }
        return snap;
    }

    private void ScanExcelCell(dynamic cell, ConversionSnapshot snap, ref int idx)
    {
        try
        {
            // U-005: flag formula cells — skip conversion, count for warning
            bool hasFormula = false;
            try { hasFormula = (bool)cell.HasFormula; } catch { }
            if (hasFormula)
            {
                snap.FormulaSkippedCount++;
                return;
            }

            string text = (string)cell.Text;
            if (string.IsNullOrEmpty(text)) return;
            string fontName = (string)cell.Font.Name;
            ProcessRun(text, fontName, snap, idx++, 0);
        }
        catch { }
    }

    // ── PowerPoint scan ───────────────────────────────────────────────────

    private ConversionSnapshot ScanPowerPoint()
    {
        var snap = new ConversionSnapshot { AppType = "PowerPoint" };
        try
        {
            var pres = _app.ActivePresentation;
            int idx = 0;
            foreach (dynamic slide in pres.Slides)
            {
                // Visible shapes on each slide
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

                // U-006: Speaker notes — NotesPage.Shapes[2] is the notes text placeholder
                try
                {
                    var notesFrame = slide.NotesPage.Shapes[2].TextFrame;
                    if ((int)notesFrame.HasText != 0)
                    {
                        foreach (dynamic notesPara in notesFrame.TextRange.Paragraphs())
                        {
                            foreach (dynamic notesRun in notesPara.Runs())
                            {
                                string text = (string)notesRun.Text;
                                string fontName = (string)notesRun.Font.Name;
                                ProcessRun(text, fontName, snap, idx++, 0);
                            }
                        }
                    }
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

    // ── Shared run processor ──────────────────────────────────────────────

    private int _processRunCallCount = 0;

    private void ProcessRun(string text, string fontName, ConversionSnapshot snap, int paraIdx, int runIdx)
    {
        // Low-memory GC hint: on machines with < 1.5 GB available RAM, collect every 500 processed
        // runs to prevent unbounded COM interop growth during large document scans.
        if (HardwareProfile.IsLowMemory)
        {
            _processRunCallCount++;
            if (_processRunCallCount % 500 == 0)
                GC.Collect();
        }

        var classification = _fontRegistry.Classify(fontName);
        switch (classification.Class)
        {
            case FontClass.Bijoy:
            {
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
            }
            case FontClass.Unsupported:
                if (!snap.UnsupportedFonts.Contains(fontName))
                    snap.UnsupportedFonts.Add(fontName);
                break;
            case FontClass.Unicode:
                if (Converter.DetectScript(text) == ScriptType.UnicodeBn)
                    snap.AlreadyUnicodeCount++;
                break;
            case FontClass.NonBengali:
            {
                var script = Converter.DetectScript(text);
                if (script == ScriptType.UnicodeBn)
                    snap.AlreadyUnicodeCount++;
                else if (script == ScriptType.Bijoy)
                {
                    var converted = _converter.Convert(text);
                    if (converted != text)
                        snap.Items.Add(new RunItem
                        {
                            Original     = text,
                            Converted    = converted,
                            FontName     = fontName,
                            ParagraphIndex = paraIdx,
                            RunIndex     = runIdx,
                            AppType      = snap.AppType,
                            SnapshotText = text,
                        });
                }
                break;
            }
        }
    }

    // ── Apply ─────────────────────────────────────────────────────────────

    public void Apply(ConversionSnapshot snapshot)
    {
        var appType = GetAppType();
        switch (appType)
        {
            case "Word":       ApplyWord(); break;
            case "Excel":      ApplyExcel(); break;
            case "PowerPoint": ApplyPowerPoint(); break;
        }
    }

    private void ApplyWord()
    {
        var doc = _app.ActiveDocument;

        void ApplyRange(dynamic range)
        {
            foreach (dynamic word in range.Words)
            {
                try
                {
                    string text = (string)word.Text;
                    string fontName = (string)word.Font.Name;
                    var cls = _fontRegistry.Classify(fontName);
                    if (cls.Class != FontClass.Bijoy) continue;
                    var converted = _converter.Convert(text.Trim());
                    if (converted == text.Trim()) continue;
                    word.Text = text.Replace(text.Trim(), converted);
                    word.Font.Name = "Noto Sans Bengali";
                }
                catch { }
            }
        }

        // Body
        foreach (dynamic para in doc.Paragraphs)
            ApplyRange(para.Range);

        // U-004: Headers and footers
        try
        {
            foreach (dynamic section in doc.Sections)
            {
                foreach (dynamic hf in section.Headers)
                    try { ApplyRange(hf.Range); } catch { }
                foreach (dynamic hf in section.Footers)
                    try { ApplyRange(hf.Range); } catch { }
            }
        }
        catch { }

        // U-004: Footnotes and endnotes
        try { foreach (dynamic fn in doc.Footnotes) try { ApplyRange(fn.Range); } catch { } } catch { }
        try { foreach (dynamic en in doc.Endnotes) try { ApplyRange(en.Range); } catch { } } catch { }

        // Word tables
        try
        {
            foreach (dynamic table in doc.Tables)
            {
                try
                {
                    foreach (dynamic row in table.Rows)
                    {
                        try
                        {
                            foreach (dynamic cell in row.Cells)
                                try { ApplyRange(cell.Range); } catch { }
                        }
                        catch { }
                    }
                }
                catch { }
            }
        }
        catch { }
    }

    private void ApplyExcel()
    {
        try
        {
            foreach (dynamic sheet in _app.ActiveWorkbook.Worksheets)
            {
                try
                {
                    foreach (dynamic cell in sheet.UsedRange.Cells)
                    {
                        try
                        {
                            bool hasFormula = false;
                            try { hasFormula = (bool)cell.HasFormula; } catch { }
                            if (hasFormula) continue;

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
                catch { }
            }
        }
        catch
        {
            var sheet = _app.ActiveSheet;
            foreach (dynamic cell in sheet.UsedRange.Cells)
            {
                try
                {
                    bool hasFormula = false;
                    try { hasFormula = (bool)cell.HasFormula; } catch { }
                    if (hasFormula) continue;
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
    }

    private void ApplyPowerPoint()
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

            // U-006: Apply to speaker notes
            try
            {
                var notesFrame = slide.NotesPage.Shapes[2].TextFrame;
                if ((int)notesFrame.HasText != 0)
                {
                    foreach (dynamic notesPara in notesFrame.TextRange.Paragraphs())
                    {
                        foreach (dynamic notesRun in notesPara.Runs())
                        {
                            try
                            {
                                string text = (string)notesRun.Text;
                                string fontName = (string)notesRun.Font.Name;
                                var cls = _fontRegistry.Classify(fontName);
                                if (cls.Class != FontClass.Bijoy) continue;
                                var converted = _converter.Convert(text);
                                if (converted == text) continue;
                                notesRun.Text = converted;
                                notesRun.Font.Name = "Noto Sans Bengali";
                            }
                            catch { }
                        }
                    }
                }
            }
            catch { }
        }
    }

    // ── Revert ─────────────────────────────────────────────────────────────

    public void Revert(ConversionSnapshot snapshot)
    {
        var appType = GetAppType();
        switch (appType)
        {
            case "Word":       RevertWord(snapshot); break;
            case "Excel":      RevertExcel(snapshot); break;
            case "PowerPoint": RevertPowerPoint(snapshot); break;
        }
    }

    private void RevertWord(ConversionSnapshot snapshot)
    {
        var doc = _app.ActiveDocument;
        var lookup = new Dictionary<string, (string orig, string font)>();
        foreach (var item in snapshot.Items)
        {
            if (!lookup.ContainsKey(item.Converted.Trim()))
                lookup[item.Converted.Trim()] = (item.Original, item.FontName);
        }

        void RevertRange(dynamic range)
        {
            foreach (dynamic word in range.Words)
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

        // Body
        foreach (dynamic para in doc.Paragraphs)
            RevertRange(para.Range);

        // U-004: Headers and footers
        try
        {
            foreach (dynamic section in doc.Sections)
            {
                foreach (dynamic hf in section.Headers)
                    try { RevertRange(hf.Range); } catch { }
                foreach (dynamic hf in section.Footers)
                    try { RevertRange(hf.Range); } catch { }
            }
        }
        catch { }

        // U-004: Footnotes and endnotes
        try { foreach (dynamic fn in doc.Footnotes) try { RevertRange(fn.Range); } catch { } } catch { }
        try { foreach (dynamic en in doc.Endnotes) try { RevertRange(en.Range); } catch { } } catch { }

        // Word tables
        try
        {
            foreach (dynamic table in doc.Tables)
            {
                try
                {
                    foreach (dynamic row in table.Rows)
                    {
                        try
                        {
                            foreach (dynamic cell in row.Cells)
                                try { RevertRange(cell.Range); } catch { }
                        }
                        catch { }
                    }
                }
                catch { }
            }
        }
        catch { }
    }

    private void RevertExcel(ConversionSnapshot snapshot)
    {
        var lookup = new Dictionary<string, (string orig, string font)>();
        foreach (var item in snapshot.Items)
        {
            if (!lookup.ContainsKey(item.Converted.Trim()))
                lookup[item.Converted.Trim()] = (item.Original, item.FontName);
        }

        void RevertSheet(dynamic sheet)
        {
            foreach (dynamic cell in sheet.UsedRange.Cells)
            {
                try
                {
                    string text = ((string)cell.Text).Trim();
                    if (lookup.TryGetValue(text, out var original))
                    {
                        cell.Value = original.orig;
                        cell.Font.Name = original.font;
                    }
                }
                catch { }
            }
        }

        try
        {
            foreach (dynamic sheet in _app.ActiveWorkbook.Worksheets)
                try { RevertSheet(sheet); } catch { }
        }
        catch
        {
            try { RevertSheet(_app.ActiveSheet); } catch { }
        }
    }

    private void RevertPowerPoint(ConversionSnapshot snapshot)
    {
        var pres = _app.ActivePresentation;
        var lookup = new Dictionary<string, (string orig, string font)>();
        foreach (var item in snapshot.Items)
        {
            if (!lookup.ContainsKey(item.Converted.Trim()))
                lookup[item.Converted.Trim()] = (item.Original, item.FontName);
        }

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
                                string text = ((string)run.Text).Trim();
                                if (lookup.TryGetValue(text, out var original))
                                {
                                    run.Text = ((string)run.Text).Replace(text, original.orig);
                                    run.Font.Name = original.font;
                                }
                            }
                            catch { }
                        }
                    }
                }
                catch { }
            }

            // U-006: Revert speaker notes
            try
            {
                var notesFrame = slide.NotesPage.Shapes[2].TextFrame;
                if ((int)notesFrame.HasText != 0)
                {
                    foreach (dynamic notesPara in notesFrame.TextRange.Paragraphs())
                    {
                        foreach (dynamic notesRun in notesPara.Runs())
                        {
                            try
                            {
                                string text = ((string)notesRun.Text).Trim();
                                if (lookup.TryGetValue(text, out var original))
                                {
                                    notesRun.Text = ((string)notesRun.Text).Replace(text, original.orig);
                                    notesRun.Font.Name = original.font;
                                }
                            }
                            catch { }
                        }
                    }
                }
            }
            catch { }
        }
    }
}
