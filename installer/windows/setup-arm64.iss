#define AppName "Mukti"
#define AppVersion "2.0.20"
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
Source: "{#BuildOutput}\api-ms-win-crt-*.dll"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
Source: "register-addin-arm64.ps1"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\Uninstall Mukti ARM64"; Filename: "{uninstallexe}"

[UninstallRun]
Filename: "powershell.exe"; Parameters: "-ExecutionPolicy Bypass -File ""{app}\register-addin-arm64.ps1"" -Uninstall"; Flags: runhidden waituntilterminated; RunOnceId: "UnregAddin"

[Code]

const
  RegBase   = 'SOFTWARE\dotnet\Setup\InstalledVersions\arm64\sharedfx\Microsoft.WindowsDesktop.App';
  RegWow    = 'SOFTWARE\WOW6432Node\dotnet\Setup\InstalledVersions\arm64\sharedfx\Microsoft.WindowsDesktop.App';
  RegBase8  = 'SOFTWARE\dotnet\Setup\InstalledVersions\arm64\sharedfx\Microsoft.WindowsDesktop.App\8.0';
  RegWow8   = 'SOFTWARE\WOW6432Node\dotnet\Setup\InstalledVersions\arm64\sharedfx\Microsoft.WindowsDesktop.App\8.0';
  RelShared = '\shared\Microsoft.WindowsDesktop.App\';

function IsDotNet8DesktopRuntimeInstalled(): Boolean;
var
  ValueNames : TArrayOfString;
  FindRec    : TFindRec;
  SharedDir  : String;
  ExitCode   : Integer;
  i          : Integer;
begin
  Result := False;

  // Layer 1: HKLM subkey (old format -- .NET <=8.0.6)
  if RegKeyExists(HKLM, RegBase8) then begin Result := True; Exit; end;
  if RegKeyExists(HKLM, RegWow8)  then begin Result := True; Exit; end;

  // Layer 2: HKCU subkey (per-user, old format)
  if RegKeyExists(HKCU, 'SOFTWARE\dotnet\Setup\InstalledVersions\arm64\sharedfx\Microsoft.WindowsDesktop.App\8.0') then
    begin Result := True; Exit; end;

  // Layer 3: Modern registry format -- version stored as VALUE name '8.0.xx'
  // on the parent key (used since .NET 8.0.7).  RegGetValueNames enumerates
  // all value names; we accept any that start with '8.'.
  if RegGetValueNames(HKLM, RegWow, ValueNames) then
    for i := 0 to GetArrayLength(ValueNames) - 1 do
      if (Length(ValueNames[i]) > 2) and (Copy(ValueNames[i], 1, 2) = '8.') then
        begin Result := True; Exit; end;

  if RegGetValueNames(HKLM, RegBase, ValueNames) then
    for i := 0 to GetArrayLength(ValueNames) - 1 do
      if (Length(ValueNames[i]) > 2) and (Copy(ValueNames[i], 1, 2) = '8.') then
        begin Result := True; Exit; end;

  // Layer 4: Filesystem -- C:\Program Files\dotnet\shared\...\8.x.y\
  SharedDir := ExpandConstant('{pf}') + '\dotnet' + RelShared;
  if DirExists(SharedDir) then
    if FindFirst(SharedDir + '8.*', FindRec) then
      begin FindClose(FindRec); Result := True; Exit; end;

  // Layer 5: Per-user MSIX / dotnet-install.ps1 -- %LOCALAPPDATA%\Microsoft\dotnet
  SharedDir := ExpandConstant('{localappdata}') + '\Microsoft\dotnet' + RelShared;
  if DirExists(SharedDir) then
    if FindFirst(SharedDir + '8.*', FindRec) then
      begin FindClose(FindRec); Result := True; Exit; end;

  // Layer 6: winget per-user -- %LOCALAPPDATA%\Programs\dotnet
  SharedDir := ExpandConstant('{localappdata}') + '\Programs\dotnet' + RelShared;
  if DirExists(SharedDir) then
    if FindFirst(SharedDir + '8.*', FindRec) then
      begin FindClose(FindRec); Result := True; Exit; end;

  // Layer 7: dotnet.exe on PATH (Scoop, Chocolatey, VS-bundled, custom prefix)
  if Exec(ExpandConstant('{cmd}'), '/C dotnet --list-runtimes', '',
          SW_HIDE, ewWaitUntilTerminated, ExitCode) then
    if ExitCode = 0 then begin Result := True; Exit; end;

  Result := False;
end;


procedure ShowArm64Notice;
var
  msg: String;
begin
  msg :=
    'Mukti ARM64 -- Preview Build' + #13#10 + #13#10 +
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
var
  dummyErrCode: Integer;
begin
  Result := True;

  // Require .NET 8 Windows Desktop Runtime (framework-dependent deployment).
  // Detection covers: registry subkeys (old format), registry values (new
  // format since 8.0.7), filesystem paths, and dotnet.exe on PATH.
  if not IsDotNet8DesktopRuntimeInstalled() then
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

  ShowArm64Notice;
end;
