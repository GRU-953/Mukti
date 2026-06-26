#define AppName "Mukti"
#define AppVersion "2.0.9"
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
DefaultDirName={autopf}\\Mukti
DefaultGroupName={#AppName}
AllowNoIcons=yes
OutputDir=output
OutputBaseFilename=Mukti-Setup-{#AppVersion}
SetupIconFile=mukti.ico
Compression=lzma2/ultra64
SolidCompression=yes
MinVersion=10.0.17763
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin
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
Source: "register-addin.ps1"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\\Uninstall Mukti"; Filename: "{uninstallexe}"

[Run]
Filename: "regsvr32"; Parameters: "/s ""{app}\\Mukti.WindowsAddin.comhost.dll"""; Flags: runhidden waituntilterminated; StatusMsg: "Registering Mukti with Windows..."
Filename: "powershell.exe"; Parameters: "-ExecutionPolicy Bypass -File ""{app}\\register-addin.ps1"" -Install -AppPath ""{app}"""; Flags: runhidden waituntilterminated; StatusMsg: "Registering Mukti with Microsoft Office..."

[UninstallRun]
Filename: "regsvr32"; Parameters: "/s /u ""{app}\\Mukti.WindowsAddin.comhost.dll"""; Flags: runhidden waituntilterminated; RunOnceId: "UnregCOM"
Filename: "powershell.exe"; Parameters: "-ExecutionPolicy Bypass -File ""{app}\\register-addin.ps1"" -Uninstall"; Flags: runhidden waituntilterminated; RunOnceId: "UnregAddin"

[Code]
function InitializeSetup(): Boolean;
var
  dummy: String;
  officeFound: Boolean;
begin
  Result := True;
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
