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
            txtSubtitle.Text = "Bijoy / SutonnyMJ â†’ Unicode Bengali";
            btnLang.Content = "à¦¬à¦¾à¦‚à¦²à¦¾";
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
            txtTitle.Text = "à¦®à§à¦•à§à¦¤à¦¿";
            txtSubtitle.Text = "à¦¬à¦¿à¦œà¦¯à¦¼ à¦¥à§‡à¦•à§‡ à¦‡à¦‰à¦¨à¦¿à¦•à§‹à¦¡ à¦°à§‚à¦ªà¦¾à¦¨à§à¦¤à¦°";
            btnLang.Content = "EN";
            txtScanBtn.Text = "à¦¸à§à¦•à§à¦¯à¦¾à¦¨ à¦•à¦°à§à¦¨";
            txtScanSelectionBtn.Text = "à¦¨à¦¿à¦°à§à¦¬à¦¾à¦šà¦¿à¦¤ à¦¸à§à¦•à§à¦¯à¦¾à¦¨ à¦•à¦°à§à¦¨";
            txtApplyBtn.Text = "à¦°à§‚à¦ªà¦¾à¦¨à§à¦¤à¦° à¦•à¦°à§à¦¨";
            txtUndoBtn.Text = "à¦ªà§‚à¦°à§à¦¬à¦¾à¦¬à¦¸à§à¦¥à¦¾à¦¯à¦¼ à¦«à§‡à¦°à¦¾à¦¨";
            txtFooter.Text = "à¦†à¦ªà¦¨à¦¾à¦° à¦¨à¦¥à¦¿à¦° à¦¬à¦¿à¦·à¦¯à¦¼à¦¬à¦¸à§à¦¤à§ à¦•à¦–à¦¨à¦“ à¦¡à¦¿à¦­à¦¾à¦‡à¦¸à§‡à¦° à¦¬à¦¾à¦‡à¦°à§‡ à¦¯à¦¾à¦¯à¦¼ à¦¨à¦¾à¥¤";
            if (colBefore != null) colBefore.Header = "à¦†à¦—à§‡";
            if (colAfter != null) colAfter.Header = "à¦ªà¦°à§‡";
        }

        if (panelUpdateBanner.Visibility == Visibility.Visible)
        {
            var match = System.Text.RegularExpressions.Regex.Match(txtUpdateBanner.Text, @"[\d.]+");
            if (match.Success)
            {
                var ver = match.Value;
                txtUpdateBanner.Text = _isEnglish
                    ? $"New version {ver} available â€” click to download"
                    : $"à¦¨à¦¤à§à¦¨ à¦¸à¦‚à¦¸à§à¦•à¦°à¦£ {ver} à¦ªà¦¾à¦“à¦¯à¦¼à¦¾ à¦—à§‡à¦›à§‡ â€” à¦¡à¦¾à¦‰à¦¨à¦²à§‹à¦¡ à¦•à¦°à¦¤à§‡ à¦•à§à¦²à¦¿à¦• à¦•à¦°à§à¦¨";
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

    private const int PreviewCap = 300;

    private void ShowScanResult(ConversionSnapshot result)
    {
        _snapshot = result;

        if (result.Items.Count == 0)
        {
            ShowStatus(_isEnglish
                ? "No Bijoy/SutonnyMJ text found."
                : "à¦•à§‹à¦¨à§‹ à¦¬à¦¿à¦œà¦¯à¦¼/à¦¸à§à¦¤à§‹à¦¨à§à¦¨à§€à¦à¦®à¦œà§‡ à¦²à§‡à¦–à¦¾ à¦ªà¦¾à¦“à¦¯à¦¼à¦¾ à¦¯à¦¾à¦¯à¦¼à¦¨à¦¿à¥¤");
            btnApply.IsEnabled = false;
        }
        else
        {
            var total = result.Items.Count;
            var displayCount = Math.Min(total, PreviewCap);
            var capped = total > PreviewCap;

            if (capped)
                ShowStatus(_isEnglish
                    ? $"Found {total} text run(s) to convert. (Showing first {PreviewCap} in preview)"
                    : $"{total}à¦Ÿà¦¿ à¦°à¦¾à¦¨ à¦°à§‚à¦ªà¦¾à¦¨à§à¦¤à¦°à§‡à¦° à¦œà¦¨à§à¦¯ à¦ªà¦¾à¦“à¦¯à¦¼à¦¾ à¦—à§‡à¦›à§‡à¥¤ (à¦ªà§à¦°à¦¥à¦® {PreviewCap}à¦Ÿà¦¿ à¦ªà§à¦°à¦¿à¦­à¦¿à¦‰à¦¤à§‡ à¦¦à§‡à¦–à¦¾à¦¨à§‹ à¦¹à¦šà§à¦›à§‡)");
            else
                ShowStatus(_isEnglish
                    ? $"Found {total} text run(s) to convert."
                    : $"{total}à¦Ÿà¦¿ à¦°à¦¾à¦¨ à¦°à§‚à¦ªà¦¾à¦¨à§à¦¤à¦°à§‡à¦° à¦œà¦¨à§à¦¯ à¦ªà¦¾à¦“à¦¯à¦¼à¦¾ à¦—à§‡à¦›à§‡à¥¤");

            for (int i = 0; i < displayCount; i++)
            {
                var item = result.Items[i];
                _previewItems.Add(new PreviewItem { Before = item.Original, After = item.Converted });
            }

            txtPreviewHeader.Visibility = Visibility.Visible;
            lstPreview.Visibility = Visibility.Visible;
            btnApply.IsEnabled = true;
        }

        var warnings = new List<string>(result.UnsupportedFonts);
        if (result.FormulaSkippedCount > 0)
            warnings.Insert(0, _isEnglish
                ? $"{result.FormulaSkippedCount} formula cell(s) skipped"
                : $"{result.FormulaSkippedCount}à¦Ÿà¦¿ à¦¸à§‚à¦¤à§à¦° à¦¸à§‡à¦² à¦¬à¦¾à¦¦ à¦¦à§‡à¦“à¦¯à¦¼à¦¾ à¦¹à¦¯à¦¼à§‡à¦›à§‡");
        if (warnings.Count > 0)
            ShowWarning(_isEnglish
                ? string.Join(" | ", warnings)
                : string.Join(" | ", warnings));
        if (result.AlreadyUnicodeCount > 0)
            ShowWarning($"{result.AlreadyUnicodeCount} run{(result.AlreadyUnicodeCount == 1 ? "" : "s")} already in Unicode Bengali — skipped.");
    }

    private async void BtnScan_Click(object sender, RoutedEventArgs e)
    {
        if (_integration == null)
        {
            ShowStatus(_isEnglish ? "No Office document open." : "à¦•à§‹à¦¨à§‹ à¦¨à¦¥à¦¿ à¦–à§‹à¦²à¦¾ à¦¨à§‡à¦‡à¥¤");
            return;
        }

        ResetForScan();
        ShowStatus(_isEnglish ? "Scanning document..." : "à¦¨à¦¥à¦¿ à¦¸à§à¦•à§à¦¯à¦¾à¦¨ à¦•à¦°à¦¾ à¦¹à¦šà§à¦›à§‡...");

        try
        {
            var result = await Task.Run(() => _integration.Scan());
            ShowScanResult(result);
        }
        catch (Exception ex)
        {
            ShowStatus((_isEnglish ? "Error: " : "à¦¤à§à¦°à§à¦Ÿà¦¿: ") + ex.Message);
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
            ShowStatus(_isEnglish ? "No Office document open." : "à¦•à§‹à¦¨à§‹ à¦¨à¦¥à¦¿ à¦–à§‹à¦²à¦¾ à¦¨à§‡à¦‡à¥¤");
            return;
        }

        ResetForScan();
        ShowStatus(_isEnglish ? "Scanning selection..." : "à¦¨à¦¿à¦°à§à¦¬à¦¾à¦šà¦¿à¦¤ à¦…à¦‚à¦¶ à¦¸à§à¦•à§à¦¯à¦¾à¦¨ à¦•à¦°à¦¾ à¦¹à¦šà§à¦›à§‡...");

        try
        {
            var result = await Task.Run(() => _integration.ScanSelection());
            ShowScanResult(result);
        }
        catch (Exception ex)
        {
            ShowStatus((_isEnglish ? "Error: " : "à¦¤à§à¦°à§à¦Ÿà¦¿: ") + ex.Message);
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
        ShowStatus(_isEnglish ? "Applying conversion..." : "à¦°à§‚à¦ªà¦¾à¦¨à§à¦¤à¦° à¦ªà§à¦°à¦¯à¦¼à§‹à¦— à¦•à¦°à¦¾ à¦¹à¦šà§à¦›à§‡...");

        try
        {
            await Task.Run(() => _integration.Apply(_snapshot));
            ShowStatus(_isEnglish
                ? $"Done â€” {_snapshot.Items.Count} run(s) converted."
                : $"à¦¸à¦®à§à¦ªà¦¨à§à¦¨ â€” {_snapshot.Items.Count}à¦Ÿà¦¿ à¦°à¦¾à¦¨ à¦°à§‚à¦ªà¦¾à¦¨à§à¦¤à¦°à¦¿à¦¤ à¦¹à¦¯à¦¼à§‡à¦›à§‡à¥¤");

            btnUndo.IsEnabled = true;
            btnUndo.Visibility = Visibility.Visible;
            lstPreview.Visibility = Visibility.Collapsed;
            txtPreviewHeader.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            ShowStatus((_isEnglish ? "Error applying: " : "à¦ªà§à¦°à¦¯à¦¼à§‹à¦—à§‡ à¦¤à§à¦°à§à¦Ÿà¦¿: ") + ex.Message);
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
        ShowStatus(_isEnglish ? "Reverting..." : "à¦«à¦¿à¦°à¦¿à¦¯à¦¼à§‡ à¦†à¦¨à¦¾ à¦¹à¦šà§à¦›à§‡...");

        try
        {
            await Task.Run(() => _integration.Revert(_snapshot));
            ShowStatus(_isEnglish ? "Reverted successfully." : "à¦ªà§‚à¦°à§à¦¬à¦¾à¦¬à¦¸à§à¦¥à¦¾à¦¯à¦¼ à¦«à§‡à¦°à¦¾à¦¨à§‹ à¦¹à¦¯à¦¼à§‡à¦›à§‡à¥¤");
            btnUndo.Visibility = Visibility.Collapsed;
            btnApply.IsEnabled = false;
            _previewItems.Clear();
            lstPreview.Visibility = Visibility.Collapsed;
            txtPreviewHeader.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            ShowStatus((_isEnglish ? "Error reverting: " : "à¦«à§‡à¦°à¦¾à¦¨à§‹à¦¯à¦¼ à¦¤à§à¦°à§à¦Ÿà¦¿: ") + ex.Message);
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
        const string currentVersion = "2.0.16";
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
                        ? $"New version {latestVer} available â€” click to download"
                        : $"à¦¨à¦¤à§à¦¨ à¦¸à¦‚à¦¸à§à¦•à¦°à¦£ {latestVer} à¦ªà¦¾à¦“à¦¯à¦¼à¦¾ à¦—à§‡à¦›à§‡ â€” à¦¡à¦¾à¦‰à¦¨à¦²à§‹à¦¡ à¦•à¦°à¦¤à§‡ à¦•à§à¦²à¦¿à¦• à¦•à¦°à§à¦¨");
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
