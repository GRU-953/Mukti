using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Mukti.Engine;

CorpusDriver.Run();

class CorpusDriver
{
    static readonly Regex WRunPat  = new(@"<w:r[ >].*?</w:r>", RegexOptions.Singleline | RegexOptions.Compiled);
    static readonly Regex WFontPat = new(@"w:(?:ascii|cs|hAnsi)=""[^""]*SutonnyMJ[^""]*""", RegexOptions.Compiled);
    static readonly Regex WTextPat = new(@"<w:t[^>]*>(.*?)</w:t>", RegexOptions.Singleline | RegexOptions.Compiled);
    static readonly Regex ARunPat  = new(@"<a:r[ >].*?</a:r>", RegexOptions.Singleline | RegexOptions.Compiled);
    static readonly Regex AFontPat = new(@"<a:(?:latin|ea|cs)[^>]*typeface=""[^""]*SutonnyMJ[^""]*""", RegexOptions.Compiled);
    static readonly Regex ATextPat = new(@"<a:t[^>]*>(.*?)</a:t>", RegexOptions.Singleline | RegexOptions.Compiled);
    static readonly Regex SiPat    = new(@"<si>.*?</si>", RegexOptions.Singleline | RegexOptions.Compiled);
    static readonly Regex SiFontPat= new(@"<rFont val=""[^""]*SutonnyMJ[^""]*""", RegexOptions.Compiled);
    static readonly Regex SiTextPat= new(@"<t[^>]*>(.*?)</t>", RegexOptions.Singleline | RegexOptions.Compiled);

    static bool IsOk(char c) =>
        (c >= 'ঀ' && c <= '৿') || c < 128 ||
        c == ' ' || c == '।' || c == '॥' ||
        (c >= 'Ͱ' && c <= 'Ͽ') ||
        (c >= '' && c <= '') ||
        c == '☐' || c == '☑' || c == '☒' ||
        c == '“' || c == '”' || c == '—' ||
        c == '–' || c == '‘' || c == '’' || c == '…' || c == '―';

    static List<string> XmlRuns(string xml, Regex runPat, Regex fontPat, Regex textPat)
    {
        var runs = new List<string>();
        foreach (Match rm in runPat.Matches(xml))
        {
            if (!fontPat.IsMatch(rm.Value)) continue;
            var text = string.Concat(textPat.Matches(rm.Value).Select(m => m.Groups[1].Value));
            if (text.Trim().Length > 0) runs.Add(text);
        }
        return runs;
    }

    static List<string> GetBijoyRuns(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        var runs = new List<string>();
        try
        {
            using var zip = ZipFile.OpenRead(path);
            foreach (var entry in zip.Entries)
            {
                string n = entry.FullName;
                bool word = ext == ".docx" && (
                    n == "word/document.xml" ||
                    n.StartsWith("word/header") || n.StartsWith("word/footer") ||
                    n == "word/footnotes.xml" || n == "word/endnotes.xml");
                bool ppt  = ext == ".pptx" && (
                    n.StartsWith("ppt/slides/slide") ||
                    n.StartsWith("ppt/notesSlides/"));
                bool xlsx = ext == ".xlsx" && n == "xl/sharedStrings.xml";
                if (!word && !ppt && !xlsx) continue;

                string xml;
                try { using var sr = new StreamReader(entry.Open(), Encoding.UTF8); xml = sr.ReadToEnd(); }
                catch { continue; }

                if (word) runs.AddRange(XmlRuns(xml, WRunPat, WFontPat, WTextPat));
                if (ppt)  runs.AddRange(XmlRuns(xml, ARunPat, AFontPat, ATextPat));
                if (xlsx)
                {
                    foreach (Match si in SiPat.Matches(xml))
                    {
                        if (!SiFontPat.IsMatch(si.Value)) continue;
                        var text = string.Concat(SiTextPat.Matches(si.Value).Select(m => m.Groups[1].Value));
                        if (text.Trim().Length > 0) runs.Add(text);
                    }
                }
            }
        }
        catch { }
        return runs;
    }

    public static void Run()
    {
        Console.OutputEncoding = Encoding.UTF8;
        var conv = new Converter(new GlyphMap(@"D:\Mukti_new\Mukti-v2\data\bijoy-sutonnymj.json"));

        var files = Directory.GetFiles(@"D:\Test_files", "*.*", SearchOption.AllDirectories)
            .Where(f => { var e = Path.GetExtension(f).ToLowerInvariant(); return e == ".docx" || e == ".xlsx" || e == ".pptx"; })
            .OrderBy(f => f)
            .ToArray();

        Console.WriteLine($"Scanning {files.Length} Office files...\n");
        var orphans = new SortedDictionary<int, List<(string file, string input, string output)>>();
        int processed = 0, runsTotal = 0;

        foreach (var file in files)
        {
            var runs = GetBijoyRuns(file);
            runsTotal += runs.Count;
            foreach (var run in runs)
            {
                var output = conv.Convert(run);
                foreach (char c in output)
                {
                    if (IsOk(c)) continue;
                    int cp = (int)c;
                    if (!orphans.ContainsKey(cp)) orphans[cp] = new();
                    if (orphans[cp].Count < 3)
                        orphans[cp].Add((Path.GetFileName(file),
                            run.Length > 60 ? run[..60] : run,
                            output.Length > 60 ? output[..60] : output));
                }
            }
            processed++;
            if (processed % 100 == 0)
                Console.Error.Write($"\r  {processed}/{files.Length} files...    ");
        }
        Console.Error.WriteLine($"\r  {files.Length}/{files.Length} done.           ");
        Console.WriteLine($"Bijoy runs found : {runsTotal}");
        Console.WriteLine($"Orphaned CP types: {orphans.Count}\n");

        if (orphans.Count == 0)
        {
            Console.WriteLine("PASS — all Bijoy text converts cleanly to Unicode Bengali.");
            return;
        }
        foreach (var kv in orphans)
        {
            Console.WriteLine($"CP {kv.Key,4} U+{kv.Key:X4} '{(char)kv.Key}'  ({kv.Value.Count} example(s)):");
            foreach (var (f, inp, outp) in kv.Value.Take(2))
            {
                Console.WriteLine($"  file: {f}");
                Console.WriteLine($"  in:   {inp}");
                Console.WriteLine($"  out:  {outp}");
            }
            Console.WriteLine();
        }
    }
}
