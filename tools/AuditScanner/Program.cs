using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using Mukti.Engine;

AuditRunner.Run(args);

static class AuditRunner
{
    // ── Regex patterns ─────────────────────────────────────────────────────────

    static readonly Regex WRunPat   = new(@"<w:r[ >].*?</w:r>",  RegexOptions.Singleline | RegexOptions.Compiled);
    static readonly Regex WFontPat  = new(@"w:(?:ascii|cs|hAnsi)=""[^""]*(?:SutonnyMJ|Bijoy)[^""]*""", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    static readonly Regex WTextPat  = new(@"<w:t[^>]*>(.*?)</w:t>", RegexOptions.Singleline | RegexOptions.Compiled);
    static readonly Regex ARunPat   = new(@"<a:r[ >].*?</a:r>",  RegexOptions.Singleline | RegexOptions.Compiled);
    static readonly Regex AFontPat  = new(@"<a:(?:latin|ea|cs)[^>]*typeface=""[^""]*(?:SutonnyMJ|Bijoy)[^""]*""", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    static readonly Regex ATextPat  = new(@"<a:t[^>]*>(.*?)</a:t>", RegexOptions.Singleline | RegexOptions.Compiled);
    static readonly Regex SiPat     = new(@"<si>.*?</si>",        RegexOptions.Singleline | RegexOptions.Compiled);
    static readonly Regex SiFontPat = new(@"<rFont val=""[^""]*(?:SutonnyMJ|Bijoy)[^""]*""", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    static readonly Regex SiTextPat = new(@"<t[^>]*>(.*?)</t>",  RegexOptions.Singleline | RegexOptions.Compiled);

    // Unicode Bengali block + safe punctuation
    static bool IsValidUnicode(char c) =>
        (c >= 'ঀ' && c <= '৿') || c < 128 ||
        c == ' ' || c == '।' || c == '॥' ||
        (c >= 'Ͱ' && c <= 'Ͽ') ||
        (c >= '‘' && c <= '‟') ||
        c == '—' || c == '–' || c == '…' || c == '―' ||
        c == '☐' || c == '☑' || c == '☒';

    // ── Entry point ─────────────────────────────────────────────────────────────

    public static void Run(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        string directory = @"D:\Test_files";
        ScanMode mode = ScanMode.Both;
        int resourceLimitPct = 70;

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--mode" && i + 1 < args.Length)
                mode = args[++i].ToLowerInvariant() switch { "bijoy" => ScanMode.Bijoy, "safety" => ScanMode.Safety, _ => ScanMode.Both };
            else if (args[i] == "--limit" && i + 1 < args.Length)
                int.TryParse(args[++i], out resourceLimitPct);
            else if (!args[i].StartsWith("--"))
                directory = args[i];
        }

        if (!Directory.Exists(directory))
        {
            Console.Error.WriteLine($"Directory not found: {directory}");
            Environment.Exit(1);
        }

        string repoRoot = FindRepoRoot() ?? AppContext.BaseDirectory;
        string dataPath = Path.Combine(repoRoot, "data", "bijoy-sutonnymj.json");

        Converter? conv = null;
        if (mode != ScanMode.Safety)
        {
            if (!File.Exists(dataPath))
            {
                Console.Error.WriteLine($"Glyph map not found: {dataPath}");
                Console.Error.WriteLine("Run from inside the repository, or specify path via --data.");
                Environment.Exit(1);
            }
            conv = new Converter(new GlyphMap(File.ReadAllText(dataPath)));
        }

        var allFiles = Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories)
            .Where(f => { var e = Path.GetExtension(f).ToLowerInvariant(); return e == ".docx" || e == ".xlsx" || e == ".pptx"; })
            .OrderBy(f => new FileInfo(f).Length)   // small files first, large last
            .ToArray();

        long memLimitBytes = (long)(GetTotalMemoryBytes() * (resourceLimitPct / 100.0));

        Console.WriteLine($"Mukti Audit Scanner");
        Console.WriteLine($"Directory  : {directory}");
        Console.WriteLine($"Files      : {allFiles.Length}");
        Console.WriteLine($"Mode       : {mode}");
        Console.WriteLine($"Mem limit  : {resourceLimitPct}% of {GetTotalMemoryBytes() / 1024 / 1024} MB");
        Console.WriteLine(new string('─', 60));

        var results = new List<FileResult>();
        var skipped = new List<string>();
        var stopwatch = Stopwatch.StartNew();
        var lastStatus = Stopwatch.StartNew();
        int processed = 0;

        foreach (var file in allFiles)
        {
            ThrottleIfNeeded(memLimitBytes);

            FileResult? result = null;
            int attempt = 0;
            while (attempt < 3 && result == null)
            {
                try
                {
                    attempt++;
                    result = ProcessFile(file, mode, conv);
                }
                catch (Exception ex) when (attempt < 3)
                {
                    Console.Error.WriteLine($"  [retry {attempt}/3] {Path.GetFileName(file)}: {ex.Message}");
                    Thread.Sleep(500);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"  [SKIP] {Path.GetFileName(file)}: {ex.Message}");
                    skipped.Add($"{Path.GetFileName(file)}: {ex.Message}");
                    break;
                }
            }

            if (result != null)
            {
                results.Add(result);
                processed++;
            }

            if (lastStatus.Elapsed.TotalMinutes >= 10)
            {
                PrintStatusUpdate(processed, allFiles.Length, results, stopwatch);
                lastStatus.Restart();
            }
        }

        PrintFinalReport(results, skipped, stopwatch);
    }

    // ── File processing ─────────────────────────────────────────────────────────

    static FileResult ProcessFile(string path, ScanMode mode, Converter? conv)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        var bijoyRuns = new List<string>();
        var orphanedCPs = new SortedDictionary<int, int>();

        using (var zip = ZipFile.OpenRead(path))
        {
            foreach (var entry in zip.Entries)
            {
                string n = entry.FullName;
                bool isWord  = ext == ".docx" && (n == "word/document.xml" || n.StartsWith("word/header") || n.StartsWith("word/footer") || n == "word/footnotes.xml" || n == "word/endnotes.xml");
                bool isPpt   = ext == ".pptx" && (n.StartsWith("ppt/slides/slide") || n.StartsWith("ppt/notesSlides/"));
                bool isXlsx  = ext == ".xlsx" && n == "xl/sharedStrings.xml";

                if (!isWord && !isPpt && !isXlsx) continue;

                string xml;
                using var sr = new StreamReader(entry.Open(), Encoding.UTF8);
                xml = sr.ReadToEnd();

                var runs = new List<string>();
                if (isWord)  runs.AddRange(ExtractRuns(xml, WRunPat, WFontPat, WTextPat));
                if (isPpt)   runs.AddRange(ExtractRuns(xml, ARunPat, AFontPat, ATextPat));
                if (isXlsx)
                {
                    foreach (Match si in SiPat.Matches(xml))
                    {
                        if (!SiFontPat.IsMatch(si.Value)) continue;
                        var t = string.Concat(SiTextPat.Matches(si.Value).Select(m => m.Groups[1].Value));
                        if (t.Trim().Length > 0) runs.Add(t);
                    }
                }

                bijoyRuns.AddRange(runs);

                if (mode != ScanMode.Bijoy && conv != null)
                {
                    foreach (var run in runs)
                    {
                        var output = conv.Convert(run);
                        foreach (char c in output)
                        {
                            if (!IsValidUnicode(c))
                            {
                                int cp = (int)c;
                                orphanedCPs.TryGetValue(cp, out int cnt);
                                orphanedCPs[cp] = cnt + 1;
                            }
                        }
                    }
                }
            }
        }

        return new FileResult(
            FileName: Path.GetFileName(path),
            FullPath: path,
            SizeBytes: new FileInfo(path).Length,
            BijoyRunCount: bijoyRuns.Count,
            OrphanedCPs: orphanedCPs
        );
    }

    static List<string> ExtractRuns(string xml, Regex runPat, Regex fontPat, Regex textPat)
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

    // ── Reporting ──────────────────────────────────────────────────────────────

    static void PrintStatusUpdate(int processed, int total, List<FileResult> results, Stopwatch sw)
    {
        double pct = processed * 100.0 / total;
        double elapsed = sw.Elapsed.TotalSeconds;
        double eta = elapsed / Math.Max(processed, 1) * (total - processed);
        int bijoyFiles = results.Count(r => r.BijoyRunCount > 0);
        int safetyFail = results.Count(r => r.OrphanedCPs.Count > 0);
        Console.WriteLine();
        Console.WriteLine($"── Status update ──────────────────────────────────────────");
        Console.WriteLine($"  Progress : {processed}/{total} ({pct:F1}%)");
        Console.WriteLine($"  ETA      : {TimeSpan.FromSeconds(eta):hh\\:mm\\:ss}");
        Console.WriteLine($"  Bijoy    : {bijoyFiles} files with Bijoy text");
        Console.WriteLine($"  Safety   : {safetyFail} files with orphaned code points");
        Console.WriteLine($"  Mem used : {GC.GetTotalMemory(false) / 1024 / 1024} MB");
        Console.WriteLine($"────────────────────────────────────────────────────────────");
    }

    static void PrintFinalReport(List<FileResult> results, List<string> skipped, Stopwatch sw)
    {
        Console.WriteLine();
        Console.WriteLine($"══ Final Report ═══════════════════════════════════════════");
        Console.WriteLine($"  Total processed : {results.Count}");
        Console.WriteLine($"  Skipped (errors): {skipped.Count}");
        Console.WriteLine($"  Elapsed         : {sw.Elapsed:hh\\:mm\\:ss}");
        Console.WriteLine();

        var bijoyFiles = results.Where(r => r.BijoyRunCount > 0).OrderByDescending(r => r.BijoyRunCount).ToList();
        Console.WriteLine($"── Bijoy Scan: {bijoyFiles.Count} file(s) contain Bijoy text ──");
        foreach (var f in bijoyFiles)
            Console.WriteLine($"  {f.BijoyRunCount,4} runs  {f.FileName}  ({f.SizeBytes / 1024} KB)");

        var safetyFails = results.Where(r => r.OrphanedCPs.Count > 0).ToList();
        Console.WriteLine();
        Console.WriteLine($"── Safety Scan: {safetyFails.Count} file(s) produced orphaned code points ──");
        foreach (var f in safetyFails)
        {
            Console.WriteLine($"  {f.FileName}:");
            foreach (var kv in f.OrphanedCPs)
                Console.WriteLine($"    U+{kv.Key:X4} '{(char)kv.Key}' × {kv.Value}");
        }

        if (skipped.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine($"── Skipped files ──");
            foreach (var s in skipped) Console.WriteLine($"  {s}");
        }

        Console.WriteLine();
        bool allClear = bijoyFiles.Count == 0 && safetyFails.Count == 0;
        Console.WriteLine(allClear
            ? "PASS — no Bijoy text found; all conversions produce valid Unicode."
            : $"REVIEW NEEDED — {bijoyFiles.Count} Bijoy file(s), {safetyFails.Count} safety failure(s).");
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    static void ThrottleIfNeeded(long memLimitBytes)
    {
        long used = GC.GetTotalMemory(false);
        if (used > memLimitBytes)
        {
            GC.Collect(2, GCCollectionMode.Aggressive, blocking: true);
            used = GC.GetTotalMemory(true);
            if (used > memLimitBytes)
                Thread.Sleep(200);
        }
    }

    static long GetTotalMemoryBytes()
    {
        try
        {
            var info = GC.GetGCMemoryInfo();
            return info.TotalAvailableMemoryBytes > 0 ? info.TotalAvailableMemoryBytes : 2L * 1024 * 1024 * 1024;
        }
        catch { return 2L * 1024 * 1024 * 1024; }
    }

    static string? FindRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir, "Mukti.sln"))) return dir;
            dir = Path.GetDirectoryName(dir);
        }
        return null;
    }

    enum ScanMode { Bijoy, Safety, Both }

    record FileResult(
        string FileName,
        string FullPath,
        long SizeBytes,
        int BijoyRunCount,
        SortedDictionary<int, int> OrphanedCPs
    );
}
