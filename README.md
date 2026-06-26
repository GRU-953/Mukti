# à¦®à§à¦•à§à¦¤à¦¿ â€” Mukti v2

**à¦¬à¦¿à¦œà¦¯à¦¼/à¦¸à§à¦Ÿà§‹à¦¨à¦¿MJ à¦¥à§‡à¦•à§‡ à¦‡à¦‰à¦¨à¦¿à¦•à§‹à¦¡ à¦¬à¦¾à¦‚à¦²à¦¾à¦¯à¦¼ à¦°à§‚à¦ªà¦¾à¦¨à§à¦¤à¦° à¦•à¦°à§à¦¨ â€” à¦¸à¦°à¦¾à¦¸à¦°à¦¿ Microsoft Office-à¦**

Convert Bijoy/SutonnyMJ Bengali text to Unicode â€” directly inside Microsoft Word, Excel, and PowerPoint.

---

## âš¡ Download & Install

### Windows (Word, Excel, PowerPoint)
1. Download **Mukti-Setup-2.0.4.exe** from [GitHub Releases](https://github.com/GRU-953/Mukti/releases)
2. Double-click the installer â€” Mukti registers itself automatically
3. Open Word (or Excel/PowerPoint) â€” the **Mukti** tab appears in the ribbon

### Mac (Word, Excel, PowerPoint)
1. Download **Mukti-2.0.4.pkg** from [GitHub Releases](https://github.com/GRU-953/Mukti/releases)
2. Right-click the .pkg â†’ **Open** (this bypasses the Gatekeeper warning â€” you only need to do this once)
3. Follow the installer steps
4. Open Word â€” the **Mukti** button appears in the Home tab

> **Mac note:** The installer is not code-signed (Apple charges $99/year). Right-click â†’ Open is safe â€” the installer contains no network code.

---

## ðŸ•¹ï¸ How to use

1. Open a Word document containing Bijoy/SutonnyMJ Bengali text
2. Click the **Mukti** tab â†’ **Mukti** button to open the task pane
3. Click **à¦¸à§à¦•à§à¦¯à¦¾à¦¨ à¦•à¦°à§à¦¨** (Scan Document) â€” Mukti finds all Bijoy-encoded runs
4. Review the before/after preview list
5. Click **à¦°à§‚à¦ªà¦¾à¦¨à§à¦¤à¦° à¦•à¦°à§à¦¨** (Apply Conversion) â€” text is replaced in-place, font changed to Noto Sans Bengali
6. Made a mistake? Click **à¦ªà§‚à¦°à§à¦¬à¦¾à¦¬à¦¸à§à¦¥à¦¾à¦¯à¦¼ à¦«à§‡à¦°à¦¾à¦¨** (Undo) to restore the original

The panel auto-detects your Office language. Set it to English with the **EN** toggle button.

---

## ðŸ”’ Privacy

**Your document content never leaves your device.**

- Windows: conversion runs inside a local COM DLL â€” no network calls
- Mac: conversion runs in WebAssembly inside Office's browser sandbox â€” no network calls
- The on-demand server on Mac (`localhost:43017`) serves only static files from your own machine; it starts when Office opens and stops when Office closes

---

## ðŸ—‚ï¸ What fonts does Mukti convert?

Mukti recognises these Bijoy/SutonnyMJ font families and converts them to Unicode:

`SutonnyMJ` Â· `SutonnyCMJ` Â· `SutonnyEMJ` Â· `GangaMJ` Â· `PadmaMJ` Â· `JomunaMJ` Â· `MeghnaMJ` Â· `TeeshTaMJ` Â· `TuragMJ` Â· `SandipanMJ` Â· `JugantorMJ` Â· `SamakalMJ` Â· `JaiJaiDinMJ` Â· and more

Fonts already in Unicode (SolaimanLipi, Noto Sans Bengali, Kohinoor Bangla, Nikoshâ€¦) are detected and left untouched.

---

## ðŸ—ï¸ Build from source

Requirements: [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

```bash
git clone https://github.com/GRU-953/Mukti.git
cd Mukti
dotnet restore Mukti.sln
dotnet build Mukti.sln --configuration Release
dotnet test src/Mukti.Engine.Tests/Mukti.Engine.Tests.csproj
```

Build the Windows installer (requires [Inno Setup 6](https://jrsoftware.org/isdl.php)):

```cmd
dotnet publish src\Mukti.WindowsAddin\Mukti.WindowsAddin.csproj --configuration Release --runtime win-x64 --self-contained true --output src\Mukti.WindowsAddin\bin\Release\net8.0-windows\win-x64\publish
iscc installer\windows\setup.iss
```

Build the Mac installer (macOS only):

```bash
bash installer/mac/build-pkg.sh
```

---

## ðŸ“‹ Known limitations

- Excel formulas containing Bengali text are skipped (counts shown in a warning â€” formula cells are never touched)
- PowerPoint SmartArt is not converted (not exposed via Office.js)
- Mac installer is not code-signed (requires Apple Developer Program, $99/year)

See [UNBUILT.md](UNBUILT.md) for the complete list of deliberate omissions.

---

## ðŸ¤ Contributing

Pull requests welcome. Before changing the mapping data (`data/bijoy-sutonnymj.json`), read [docs/DECISION-LOG.md](docs/DECISION-LOG.md) â€” especially D-0003 (corpus-gate rule).

All mapping changes must pass the corpus gate: `dotnet test` must report **â‰¥99% pass rate** before merging.

---

## ðŸ“œ Licence

MIT â€” see [LICENSE](LICENSE)

---

*à¦®à§à¦•à§à¦¤à¦¿ à¦®à¦¾à¦¨à§‡ à¦¸à§à¦¬à¦¾à¦§à§€à¦¨à¦¤à¦¾à¥¤ Mukti means freedom.*

