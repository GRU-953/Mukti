#define AppName "Mukti"
#define AppVersion "2.0.19"
#define AppPublisher "GRU-953"
#define AppURL "https://github.com/GRU-953/Mukti"
#define AppGuid "F4E71C21-9B7A-4C3E-8D22-8F91A235C4B1"
#define BuildOutput "..\\..\\src\\Mukti.WindowsAddin\\bin\\Release\\net8.0-windows\\win-x64\\publish"

[Setup]
AppId={{{#AppGuid}}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppURL}
AppSupportURL={#AppURL}
AppUpdatesURL={#AppURL}
; Per-user install (no admin elevation). A .NET COM add-in registers itself and
; its Office Addins keys under HKEY_CURRENT_USER. If the installer elevated to
; admin, "current user" would be the ADMIN account and the logged-in user's Word/
; Excel/PowerPoint would never see the add-in. Installing per-user keeps every
; registration in the real user's hive, which is where Office looks. It also
; removes the UAC prompt â€” friendlier for non-technical users.
DefaultDirName={localappdata}\\Mukti
DefaultGroupName={#AppName}
AllowNoIcons=yes
OutputDir=output
OutputBaseFilename=Mukti-Setup-{#AppVersion}
SetupIconFile=mukti.ico
Compression=lzma2/ultra64
SolidCompression=yes
MinVersion=10.0.17763
ArchitecturesAllowed=x64compatible
; Install in 64-bit mode so regsvr32 and registry access use the 64-bit view,
; matching the x64 add-in and 64-bit Office.
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=lowest
CloseApplications=yes
CloseApplicationsFilter=*.exe

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
Source: "{#BuildOutput}\\Mukti.WindowsAddin.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildOutput}\\Mukti.WindowsAddin.comhost.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildOutput}\\Mukti.Engine.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildOutput}\\libs\\office.dll"; DestDir: "{app}\\libs"; Flags: ignoreversion skipifsourcedoesntexist
Source: "{#BuildOutput}\\libs\\Extensibility.dll"; DestDir: "{app}\\libs"; Flags: ignoreversion skipifsourcedoesntexist
Source: "..\\..\\data\\bijoy-sutonnymj.json"; DestDir: "{app}\\data"; Flags: ignoreversion
Source: "{#BuildOutput}\\*.dll"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
Source: "{#BuildOutput}\\*.json"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
; The .NET comhost reads this sidecar to map CLSIDs to managed types at runtime.
; dotnet publish does not copy it automatically â€” the csproj CopyClsidMapToPublish target does.
Source: "{#BuildOutput}\\*.clsidmap"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
; Override the auto-generated runtimeconfig with the framework-dependent version.
; dotnet publish --self-contained generates "includedFrameworks" which the .NET comhost
; cannot initialize during COM DllGetClassObject. "frameworks" (system runtime) works.
Source: "Mukti.WindowsAddin.runtimeconfig.json"; DestDir: "{app}"; Flags: ignoreversion
; UCRT stub DLLs: Microsoft 365 Click-to-Run's virtual file system does not expose
; C:\Windows\System32\downlevel to hosted COM DLLs. Without these stubs in the same
; directory, the .NET comhost fails to load inside Word/Excel/PowerPoint.
; The csproj CopyUCRTStubs target copies them from the build machine during publish.
Source: "{#BuildOutput}\\api-ms-win-crt-*.dll"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
Source: "register-addin.ps1"; DestDir: "{app}"; Flags: ignoreversion
Source: "fix-mukti-registration.ps1"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\\Repair Mukti (if it does not appear in Office)"; Filename: "powershell.exe"; Parameters: "-ExecutionPolicy Bypass -File ""{app}\\fix-mukti-registration.ps1"""; IconFilename: "{app}\\Mukti.WindowsAddin.dll"
Name: "{group}\\Uninstall Mukti"; Filename: "{uninstallexe}"

[Run]
; register-addin.ps1 writes both the HKCU COM registration (CLSID/InprocServer32/ProgId)
; and the Office Addins keys directly. regsvr32 is not used because it attempts HKLM
; writes first and returns exit code 5 (access denied) in a non-elevated process.
Filename: "powershell.exe"; Parameters: "-ExecutionPolicy Bypass -WindowStyle Hidden -File ""{app}\\register-addin.ps1"" -Install -AppPath ""{app}"""; Flags: runhidden waituntilterminated; StatusMsg: "Registering Mukti with Microsoft Office..."

[UninstallRun]
Filename: "powershell.exe"; Parameters: "-ExecutionPolicy Bypass -WindowStyle Hidden -File ""{app}\\register-addin.ps1"" -Uninstall"; Flags: runhidden waituntilterminated; RunOnceId: "UnregAddin"

[Code]
function InitializeSetup(): Boolean;
var
  dummy: String;
  officeFound: Boolean;
  dummyErrCode: Integer;
begin
  Result := True;

  // Block the per-machine -> per-user migration trap. Versions up to 2.0.10 installed
  // for ALL USERS (admin) and recorded their uninstaller in HKLM. This per-user
  // installer (PrivilegesRequired=lowest) cannot see or remove that older copy, so
  // installing over it would leave two copies and a stale machine-wide COM
  // registration that can still shadow this one. Detect the old install and stop
  // with a clear instruction rather than silently producing a broken state.
  if RegKeyExists(HKLM, 'SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{F4E71C21-9B7A-4C3E-8D22-8F91A235C4B1}_is1') or
     RegKeyExists(HKLM32, 'SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{F4E71C21-9B7A-4C3E-8D22-8F91A235C4B1}_is1') then
  begin
    MsgBox(
      'An older version of Mukti is installed for all users on this computer.' + #13#10 +
      'It must be removed first (it was installed with administrator rights).' + #13#10 + #13#10 +
      'Please open  Windows Settings > Apps > Installed apps,  find "Mukti",' + #13#10 +
      'choose Uninstall, then run this installer again.' + #13#10 + #13#10 +
      'This new version installs just for you and needs no administrator permission.',
      mbError, MB_OK);
    Result := False;
    exit;
  end;

  // Require .NET 8 WindowsDesktop Runtime (framework-dependent deployment).
  // Without it the comhost cannot initialize the managed add-in.
  if not RegKeyExists(HKLM, 'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.WindowsDesktop.App\8.0') and
     not RegKeyExists(HKLM, 'SOFTWARE\WOW6432Node\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.WindowsDesktop.App\8.0') then
  begin
    if MsgBox(
      'Mukti needs the .NET 8 Desktop Runtime to work.' + #13#10 +
      'It is free and provided by Microsoft.' + #13#10 + #13#10 +
      'Click Yes to open the download page.' + #13#10 +
      'After installing .NET 8, run this setup again.',
      mbConfirmation, MB_YESNO) = IDYES then
    begin
      ShellExec('open', 'https://dotnet.microsoft.com/en-us/download/dotnet/8.0/runtime', '', '', SW_SHOWNORMAL, ewNoWait, dummyErrCode);
    end;
    Result := False;
    exit;
  end;

  // Detect any supported Office installation:
  //  - Click-to-Run  (Microsoft 365, Office 2019, Office 2021)
  //  - MSI 64-bit    (traditional volume/retail installs)
  //  - MSI 32-bit    (32-bit Office on 64-bit Windows, lives under WOW6432Node)
  //  - Per-user      (HKCU install path)
  //  - Office 2013   (version 15.0)
  officeFound :=
    RegKeyExists(HKLM, 'SOFTWARE\Microsoft\Office\ClickToRun\Configuration') or
    RegQueryStringValue(HKLM, 'SOFTWARE\Microsoft\Office\16.0\Common\InstallRoot', 'Path', dummy) or
    RegQueryStringValue(HKLM32, 'SOFTWARE\Microsoft\Office\16.0\Common\InstallRoot', 'Path', dummy) or
    RegQueryStringValue(HKCU, 'SOFTWARE\Microsoft\Office\16.0\Common\InstallRoot', 'Path', dummy) or
    RegKeyExists(HKLM, 'SOFTWARE\Microsoft\Office\15.0\Common') or
    RegKeyExists(HKLM32, 'SOFTWARE\Microsoft\Office\15.0\Common');

  if not officeFound then
    if MsgBox(
      'Microsoft Office was not detected on this computer.' + #13#10 +
      'Mukti requires Word, Excel, or PowerPoint 2013 or later.' + #13#10 + #13#10 +
      'Continue the installation anyway?',
      mbConfirmation, MB_YESNO) = IDNO then
      Result := False;
end;
