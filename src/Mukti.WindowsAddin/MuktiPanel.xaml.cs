using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace Mukti.WindowsAddin;

public partial class MuktiPanel : System.Windows.Controls.UserControl
{
    private readonly dynamic? _app;
    private bool _isEnglish = false;
    private OfficeIntegration? _integration;
    private ObservableCollection<PreviewItem> _previewItems = new();
    private ConversionSnapshot? _snapshot;

    public bool IsEnglish => _isEnglish;

    public MuktiPanel(dynamic? app = null)
    {
        InitializeComponent();
        _app = app;
        _integration = app != null ? new OfficeIntegration(app) : null;
        lstPreview.ItemsSource = _previewItems;
        CheckForUpdateAsync();
    }

    public void SetLanguage(bool english)
    {
        _isEnglish = english;
        UpdateLanguage();
    }

    private void UpdateLanguage()
    {
        if (_isEnglish)
        {
            txtTitle.Text = "Mukti";
            txtSubtitle.Text = "Bijoy / SutonnyMJ → Unicode Bengali";
            btnLang.Content = "বাংলা";
            txtScanBtn.Text = "Scan Document";
            txtScanSelectionBtn.Text = "Scan Selection";
            txtApplyBtn.Text = "Apply Conversion";
            txtUndoBtn.Text = "Undo Conversion";
            txtFooter.Text = "Your document content never leaves your device.";
            if (colBefore != null) colBefore.Header = "Before";
            if (colAfter != null) colAfter.Header = "After";
        }
        else
        {
            txtTitle.Text = "মুক্তি";
            txtSubtitle.Text = "বিজয় থেকে ইউনিকোড রূপান্তর";
            btnLang.Content = "EN";
            txtScanBtn.Text = "স্ক্যান করুন";
            txtScanSelectionBtn.Text = "নির্বাচিত স্ক্যান করুন";
            txtApplyBtn.Text = "রূপান্তর করুন";
            txtUndoBtn.Text = "পূর্বাবস্থায় ফেরান";
            txtFooter.Text = "আপনার নথির বিষয়বস্তু কখনও ডিভাইসের বাইরে যায় না।";
            if (colBefore != null) colBefore.Header = "আগে";
            if (colAfter != null) colAfter.Header = "পরে";
        }

        if (panelUpdateBanner.Visibility == Visibility.Visible)
        {
            var match = System.Text.RegularExpressions.Regex.Match(txtUpdateBanner.Text, @"[\d.]+");
            if (match.Success)
            {
                var ver = match.Value;
                txtUpdateBanner.Text = _isEnglish
                    ? $"New version {ver} available — click to download"
                    : $"নতুন সংস্করণ {ver} পাওয়া গেছে — ডাউনলোড করতে ক্লিক করুন";
            }
        }
    }

    private void BtnLang_Click(object sender, RoutedEventArgs e)
    {
        _isEnglish = !_isEnglish;
        UpdateLanguage();
    }

    private void ShowStatus(string message)
    {
        txtStatus.Text = message;
        panelStatus.Visibility = Visibility.Visible;
    }

    private void ShowWarning(string message)
    {
        txtWarning.Text = message;
        panelWarning.Visibility = Visibility.Visible;
    }

    private void HideWarning()
    {
        panelWarning.Visibility = Visibility.Collapsed;
    }

    private void ResetForScan()
    {
        btnScan.IsEnabled = false;
        btnScanSelection.IsEnabled = false;
        btnApply.IsEnabled = false;
        HideWarning();
        _previewItems.Clear();
        lstPreview.Visibility = Visibility.Collapsed;
        txtPreviewHeader.Visibility = Visibility.Collapsed;
    }

    private void ShowScanResult(ConversionSnapshot result)
    {
        _snapshot = result;

        if (result.Items.Count == 0)
        {
            ShowStatus(_isEnglish
                ? "No Bijoy/SutonnyMJ text found."
                : "কোনো বিজয়/সুতোন্নীএমজে লেখা পাওয়া যায়নি।");
            btnApply.IsEnabled = false;
        }
        else
        {
            ShowStatus(_isEnglish
                ? $"Found {result.Items.Count} text run(s) to convert."
                : $"{result.Items.Count}টি রান রূপান্তরের জন্য পাওয়া গেছে।");

            foreach (var item in result.Items)
                _previewItems.Add(new PreviewItem { Before = item.Original, After = item.Converted });

            txtPreviewHeader.Visibility = Visibility.Visible;
            lstPreview.Visibility = Visibility.Visible;
            btnApply.IsEnabled = true;
        }

        var warnings = new List<string>(result.UnsupportedFonts);
        if (result.FormulaSkippedCount > 0)
            warnings.Insert(0, _isEnglish
                ? $"{result.FormulaSkippedCount} formula cell(s) skipped"
                : $"{result.FormulaSkippedCount}টি সূত্র সেল বাদ দেওয়া হয়েছে");
        if (warnings.Count > 0)
            ShowWarning(_isEnglish
                ? string.Join(" | ", warnings)
                : string.Join(" | ", warnings));
    }

    private async void BtnScan_Click(object sender, RoutedEventArgs e)
    {
        if (_integration == null)
        {
            ShowStatus(_isEnglish ? "No Office document open." : "কোনো নথি খোলা নেই।");
            return;
        }

        ResetForScan();
        ShowStatus(_isEnglish ? "Scanning document..." : "নথি স্ক্যান করা হচ্ছে...");

        try
        {
            var result = await Task.Run(() => _integration.Scan());
            ShowScanResult(result);
        }
        catch (Exception ex)
        {
            ShowStatus((_isEnglish ? "Error: " : "ত্রুটি: ") + ex.Message);
        }
        finally
        {
            btnScan.IsEnabled = true;
            btnScanSelection.IsEnabled = true;
        }
    }

    // U-011: Scan selection only
    private async void BtnScanSelection_Click(object sender, RoutedEventArgs e)
    {
        if (_integration == null)
        {
            ShowStatus(_isEnglish ? "No Office document open." : "কোনো নথি খোলা নেই।");
            return;
        }

        ResetForScan();
        ShowStatus(_isEnglish ? "Scanning selection..." : "নির্বাচিত অংশ স্ক্যান করা হচ্ছে...");

        try
        {
            var result = await Task.Run(() => _integration.ScanSelection());
            ShowScanResult(result);
        }
        catch (Exception ex)
        {
            ShowStatus((_isEnglish ? "Error: " : "ত্রুটি: ") + ex.Message);
        }
        finally
        {
            btnScan.IsEnabled = true;
            btnScanSelection.IsEnabled = true;
        }
    }

    private async void BtnApply_Click(object sender, RoutedEventArgs e)
    {
        if (_integration == null || _snapshot == null) return;

        btnApply.IsEnabled = false;
        btnScan.IsEnabled = false;
        btnScanSelection.IsEnabled = false;
        ShowStatus(_isEnglish ? "Applying conversion..." : "রূপান্তর প্রয়োগ করা হচ্ছে...");

        try
        {
            await Task.Run(() => _integration.Apply(_snapshot));
            ShowStatus(_isEnglish
                ? $"Done — {_snapshot.Items.Count} run(s) converted."
                : $"সম্পন্ন — {_snapshot.Items.Count}টি রান রূপান্তরিত হয়েছে।");

            btnUndo.IsEnabled = true;
            btnUndo.Visibility = Visibility.Visible;
            lstPreview.Visibility = Visibility.Collapsed;
            txtPreviewHeader.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            ShowStatus((_isEnglish ? "Error applying: " : "প্রয়োগে ত্রুটি: ") + ex.Message);
            btnApply.IsEnabled = true;
        }
        finally
        {
            btnScan.IsEnabled = true;
            btnScanSelection.IsEnabled = true;
        }
    }

    private async void BtnUndo_Click(object sender, RoutedEventArgs e)
    {
        if (_integration == null || _snapshot == null) return;

        btnUndo.IsEnabled = false;
        ShowStatus(_isEnglish ? "Reverting..." : "ফিরিয়ে আনা হচ্ছে...");

        try
        {
            await Task.Run(() => _integration.Revert(_snapshot));
            ShowStatus(_isEnglish ? "Reverted successfully." : "পূর্বাবস্থায় ফেরানো হয়েছে।");
            btnUndo.Visibility = Visibility.Collapsed;
            btnApply.IsEnabled = false;
            _previewItems.Clear();
            lstPreview.Visibility = Visibility.Collapsed;
            txtPreviewHeader.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            ShowStatus((_isEnglish ? "Error reverting: " : "ফেরানোয় ত্রুটি: ") + ex.Message);
            btnUndo.IsEnabled = true;
        }
    }

    private void BtnUpdate_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://github.com/GRU-953/Mukti/releases/latest") { UseShellExecute = true }); }
        catch { }
    }

    private async void CheckForUpdateAsync()
    {
        const string currentVersion = "2.0.4";
        try
        {
            using var client = new System.Net.Http.HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "Mukti-Addin");
            client.Timeout = TimeSpan.FromSeconds(5);
            var json = await client.GetStringAsync("https://api.github.com/repos/GRU-953/Mukti/releases/latest");
            var tagMatch = System.Text.RegularExpressions.Regex.Match(json, @"""tag_name""\s*:\s*""v?([\d.]+)""");
            if (!tagMatch.Success) return;
            var latestVer = tagMatch.Groups[1].Value;
            if (new Version(latestVer) > new Version(currentVersion))
            {
                Dispatcher.Invoke(() =>
                {
                    txtUpdateBanner.Text = (_isEnglish
                        ? $"New version {latestVer} available — click to download"
                        : $"নতুন সংস্করণ {latestVer} পাওয়া গেছে — ডাউনলোড করতে ক্লিক করুন");
                    panelUpdateBanner.Visibility = Visibility.Visible;
                });
            }
        }
        catch { }
    }
}

public class PreviewItem
{
    public string Before { get; set; } = "";
    public string After { get; set; } = "";
}
