using System.Windows.Forms;
using System.Windows.Forms.Integration;
using System.Globalization;

namespace Mukti.WindowsAddin;

[System.Runtime.InteropServices.ComVisible(true)]
[System.Runtime.InteropServices.Guid("A3B7D912-5E4F-4A21-9C8E-1F2A3B4C5D6E")]
public class MuktiTaskPaneControl : UserControl
{
    private readonly ElementHost _host;
    private readonly MuktiPanel _panel;
    private readonly dynamic? _officeApp;

    public bool IsEnglish => _panel.IsEnglish;

    public MuktiTaskPaneControl(dynamic? officeApp = null)
    {
        _officeApp = officeApp;
        _panel = new MuktiPanel(officeApp);

        // Detect language from Office or system locale
        bool useBangla = DetectBangla();
        _panel.SetLanguage(!useBangla); // IsEnglish = !useBangla

        _host = new ElementHost
        {
            Dock = DockStyle.Fill,
            Child = _panel
        };

        Controls.Add(_host);
        BackColor = System.Drawing.Color.FromArgb(255, 253, 245, 230);
    }

    private bool DetectBangla()
    {
        try
        {
            // Try to read Office's language setting
            if (_officeApp != null)
            {
                // MsoLanguageID for Bengali (India) = 1093
                // MsoLanguageID for Bengali (Bangladesh) = 2117
                var langId = (int)_officeApp.LanguageSettings
                    .LanguagePreferredForEditing(1033); // Check if English preferred
                return langId == 1093 || langId == 2117;
            }
        }
        catch { }

        // Fallback: system culture
        var ci = CultureInfo.CurrentUICulture;
        return ci.TwoLetterISOLanguageName == "bn";
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) { _host.Dispose(); }
        base.Dispose(disposing);
    }
}
