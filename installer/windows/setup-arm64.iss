#define AppName "Mukti"
#define AppVersion "2.0.11"
#define AppPublisher "GRU-953"
#define AppURL "https://github.com/GRU-953/Mukti"
#define AppGuid "A7B3C9D1-2E4F-5A6B-7C8D-9E0F1A2B3C4D"
#define BuildOutput "..\\..\\src\\Mukti.WindowsAddin\\bin\\Release\\net8.0-windows\\win-arm64\\publish"

[Setup]
AppId={{{#AppGuid}}
AppName={#AppName} (ARM64 Preview)
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppURL}
AppSupportURL={#AppURL}
AppUpdatesURL={#AppURL}
DefaultDirName={autopf}\Mukti\arm64
DefaultGroupName={#AppName} ARM64
AllowNoIcons=yes
OutputDir=output
OutputBaseFilename=Mukti-Setup-{#AppVersion}-arm64
SetupIconFile=mukti.ico
Compression=lzma2/ultra64
SolidCompression=yes
MinVersion=10.0.17763
ArchitecturesAllowed=arm64
ArchitecturesInstallIn64BitMode=arm64
PrivilegesRequired=admin

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
Source: "{#BuildOutput}\Mukti.WindowsAddin.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildOutput}\Mukti.WindowsAddin.comhost.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildOutput}\Mukti.Engine.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildOutput}\office.dll"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
Source: "{#BuildOutput}\Extensibility.dll"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
Source: "..\..\data\bijoy-sutonnymj.json"; DestDir: "{app}\data"; Flags: ignoreversion
Source: "{#BuildOutput}\*.dll"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
Source: "{#BuildOutput}\*.json"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
Source: "{#BuildOutput}\*.clsidmap"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
Source: "register-addin-arm64.ps1"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\Uninstall Mukti ARM64"; Filename: "{uninstallexe}"

[UninstallRun]
Filename: "powershell.exe"; Parameters: "-ExecutionPolicy Bypass -File ""{app}\register-addin-arm64.ps1"" -Uninstall"; Flags: runhidden waituntilterminated; RunOnceId: "UnregAddin"

[Code]
procedure ShowArm64Notice;
var
  msg: String;
begin
  msg :=
    'Mukti ARM64 — Preview Build' + #13#10 + #13#10 +
    'Microsoft Office does not yet ship a native ARM64 build.' + #13#10 +
    'On Windows ARM64 devices, Office currently runs as x64 under emulation.' + #13#10 + #13#10 +
    'This installer copies the ARM64 binaries to:' + #13#10 +
    '  %ProgramFiles%\Mukti\arm64\' + #13#10 + #13#10 +
    'COM registration is NOT performed automatically.' + #13#10 +
    'When ARM64-native Office becomes available, run:' + #13#10 +
    '  register-addin-arm64.ps1 -Install' + #13#10 + #13#10 +
    'To use Mukti with Office today, install the x64 build instead.' + #13#10 + #13#10 +
    'Continue installing the ARM64 preview?';

  if MsgBox(msg, mbConfirmation, MB_YESNO) = IDNO then
    Abort;
end;

function InitializeSetup(): Boolean;
begin
  Result := True;
  ShowArm64Notice;
end;
