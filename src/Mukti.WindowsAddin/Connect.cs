using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Extensibility;
using Microsoft.Office.Core;
using Mukti.Engine;

namespace Mukti.WindowsAddin;

[ComVisible(true)]
[Guid("F4E71C21-9B7A-4C3E-8D22-8F91A235C4B1")]
[ProgId("Mukti.Connect")]
[ClassInterface(ClassInterfaceType.AutoDual)]
public class Connect : IDTExtensibility2, IRibbonExtensibility
{
    private dynamic? _application;
    private IRibbonUI? _ribbon;
    private Form? _panelForm;
    private MuktiTaskPaneControl? _panelControl;

    // ── IDTExtensibility2 ─────────────────────────────────────────────────

    public void OnConnection(object Application, ext_ConnectMode ConnectMode,
                             object AddInInst, ref Array custom)
    {
        _application = Application;
    }

    public void OnStartupComplete(ref Array custom) { }

    public void OnDisconnection(ext_DisconnectMode RemoveMode, ref Array custom)
    {
        try { _panelForm?.Close(); _panelForm?.Dispose(); _panelControl?.Dispose(); } catch { }
        _panelForm = null;
        _panelControl = null;
    }

    public void OnAddInsUpdate(ref Array custom) { }
    public void OnBeginShutdown(ref Array custom) { }

    // ── IRibbonExtensibility ──────────────────────────────────────────────

    public string GetCustomUI(string RibbonID)
    {
        var asm = Assembly.GetExecutingAssembly();
        var resourceName = "Mukti.WindowsAddin.Resources.ribbon.xml";
        using var stream = asm.GetManifestResourceStream(resourceName)
            ?? throw new FileNotFoundException("ribbon.xml not found in resources");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    public void OnRibbonLoad(IRibbonUI ribbonUI)
    {
        _ribbon = ribbonUI;
    }

    public bool GetTaskPaneVisible(IRibbonControl control)
        => _panelForm != null && !_panelForm.IsDisposed && _panelForm.Visible;

    public void ToggleTaskPane(IRibbonControl control, bool isPressed)
    {
        if (isPressed)
        {
            if (_panelForm == null || _panelForm.IsDisposed)
            {
                _panelControl = new MuktiTaskPaneControl(_application);
                _panelControl.Dock = DockStyle.Fill;

                _panelForm = new Form
                {
                    Text = "Mukti",
                    Width = 380,
                    Height = 640,
                    MinimumSize = new System.Drawing.Size(300, 400),
                    FormBorderStyle = FormBorderStyle.SizableToolWindow,
                    StartPosition = FormStartPosition.WindowsDefaultLocation,
                    ShowInTaskbar = false,
                };
                _panelForm.Controls.Add(_panelControl);
                _panelForm.FormClosed += (s, e) => { try { _ribbon?.InvalidateControl("btnMukti"); } catch { } };
            }
            _panelForm.Show();
            _panelForm.Activate();
        }
        else
        {
            _panelForm?.Hide();
        }
    }

    // ── Engine factory ───────────────────────────────────────────────────

    private static Converter? _converter;
    private static FontRegistry? _fontRegistry;

    internal static Converter GetConverter()
    {
        if (_converter != null) return _converter;
        var dataPath = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
            "data", "bijoy-sutonnymj.json");
        _converter = new Converter(new GlyphMap(dataPath));
        return _converter;
    }

    internal static FontRegistry GetFontRegistry()
    {
        _fontRegistry ??= new FontRegistry();
        return _fontRegistry;
    }
}
