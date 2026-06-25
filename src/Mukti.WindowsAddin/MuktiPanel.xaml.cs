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
            txtApplyBtn.Text = "রূপান্তর করুন";
            txtUndoBtn.Text = "পূর্বাবস্থায় ফেরান";
            txtFooter.Text = "আপনার নথির বিষয়বস্তু কখনও ডিভাইসের বাইরে যায় না।";
            if (colBefore != null) colBefore.Header = "আগে";
            if (colAfter != null) colAfter.Header = "পরে";
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

    private async void BtnScan_Click(object sender, RoutedEventArgs e)
    {
        if (_integration == null)
        {
            ShowStatus(_isEnglish ? "No Office document open." : "কোনো নথি খোলা নেই।");
            return;
        }

        btnScan.IsEnabled = false;
        btnApply.IsEnabled = false;
        HideWarning();
        _previewItems.Clear();
        lstPreview.Visibility = Visibility.Collapsed;
        txtPreviewHeader.Visibility = Visibility.Collapsed;

        ShowStatus(_isEnglish ? "Scanning document..." : "নথি স্ক্যান করা হচ্ছে...");

        try
        {
            var result = await Task.Run(() => _integration.Scan());
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

            if (result.UnsupportedFonts.Count > 0)
            {
                var fontList = string.Join(", ", result.UnsupportedFonts);
                ShowWarning(_isEnglish
                    ? $"Unrecognised fonts (not converted): {fontList}"
                    : $"অজানা ফন্ট (রূপান্তর হয়নি): {fontList}");
            }
        }
        catch (Exception ex)
        {
            ShowStatus((_isEnglish ? "Error: " : "ত্রুটি: ") + ex.Message);
        }
        finally
        {
            btnScan.IsEnabled = true;
        }
    }

    private async void BtnApply_Click(object sender, RoutedEventArgs e)
    {
        if (_integration == null || _snapshot == null) return;

        btnApply.IsEnabled = false;
        btnScan.IsEnabled = false;
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
}

public class PreviewItem
{
    public string Before { get; set; } = "";
    public string After { get; set; } = "";
}
