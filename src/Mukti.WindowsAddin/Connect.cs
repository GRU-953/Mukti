// Connect.cs — COM add-in entry point for Mukti v2
// Implements IDTExtensibility2 + IRibbonExtensibility + ICustomTaskPaneConsumer
// Registers for Word, Excel, and PowerPoint via the Inno Setup installer.

using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Extensibility;
using Microsoft.Office.Core;
using Mukti.Engine;

namespace Mukti.WindowsAddin;

[ComVisible(true)]
[Guid("F4E71C21-9B7A-4C3E-8D22-8F91A235C4B1")]
[ProgId("Mukti.Connect")]
[ClassInterface(ClassInterfaceType.None)]
public class Connect : IDTExtensibility2, IRibbonExtensibility, ICustomTaskPaneConsumer
{
    private dynamic? _application;
    private ICTPFactory? _ctpCollection;
    private _CustomTaskPane? _taskPane;
    private MuktiTaskPaneControl? _taskPaneControl;
    private IRibbonUI? _ribbon;

    // ── IDTExtensibility2 ─────────────────────────────────────────────────

    public void OnConnection(object Application, ext_ConnectMode ConnectMode,
                             object AddInInst, ref Array custom)
    {
        _application = Application;
        if (ConnectMode == ext_ConnectMode.ext_cm_AfterStartup)
            CreateTaskPane();
    }

    public void OnStartupComplete(ref Array custom)
    {
        CreateTaskPane();
    }

    public void OnDisconnection(ext_DisconnectMode RemoveMode, ref Array custom)
    {
        _taskPane?.Delete();
        _taskPaneControl?.Dispose();
        _taskPane = null;
        _taskPaneControl = null;
        _ctpCollection = null;
    }

    public void OnAddInsUpdate(ref Array custom) { }
    public void OnBeginShutdown(ref Array custom) { }

    // ── IRibbonExtensibility ──────────────────────────────────────────────

    public string GetCustomUI(string RibbonID)
    {
        // Load ribbon.xml from embedded resources
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
        => _taskPane?.Visible ?? false;

    public string GetGroupLabel(IRibbonControl control)
    {
        // Return "বাংলা" or "Bengali" based on language setting
        return _taskPaneControl?.IsEnglish == true ? "Bengali" : "বাংলা";
    }

    public System.Drawing.Bitmap GetRibbonImage(IRibbonControl control)
    {
        using var stream = GetType().Assembly.GetManifestResourceStream("Mukti.WindowsAddin.Resources.icon32.png")!;
        return new System.Drawing.Bitmap(stream);
    }

    public void ToggleTaskPane(IRibbonControl control, bool isPressed)
    {
        if (_taskPane != null)
            _taskPane.Visible = isPressed;
    }

    // ── ICustomTaskPaneConsumer ───────────────────────────────────────────

    public void CTPFactoryAvailable(ICTPFactory CTPFactoryInst)
    {
        _ctpCollection = CTPFactoryInst;
        CreateTaskPaneWithFactory(CTPFactoryInst);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private void CreateTaskPane()
    {
        // Called from OnStartupComplete; task pane may already be created via CTPFactoryAvailable
        if (_taskPane != null) return;
    }

    private void CreateTaskPaneWithFactory(ICTPFactory factory)
    {
        if (_taskPane != null) return;

        _taskPaneControl = new MuktiTaskPaneControl(_application);
        _taskPane = factory.CreateCTP(
            typeof(MuktiTaskPaneControl).FullName!,
            "Mukti",
            Type.Missing) as _CustomTaskPane;

        if (_taskPane != null)
        {
            _taskPane.Width = 320;
            _taskPane.Visible = false;
            if (_taskPane is _CustomTaskPaneEvents_Event evtPane)
            {
                evtPane.VisibleStateChange += (pane) =>
                {
                    _ribbon?.InvalidateControl("btnMukti");
                };
            }
        }
    }

    // ── Engine factory (lazily loads the glyph map) ───────────────────────

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
