# fix-mukti-registration.ps1
# Repairs Mukti when it was installed but does not appear in Word/Excel/PowerPoint.
#
# Run this as yourself — do NOT "Run as administrator" (that would write registry
# keys into the administrator account, not yours, and Word still wouldn't see them).
#
# How to run: right-click this file -> "Run with PowerShell".

$ErrorActionPreference = 'Stop'
$ProgId  = 'Mukti.Connect'
$Clsid   = '{F4E71C21-9B7A-4C3E-8D22-8F91A235C4B1}'
$apps    = @('Word', 'Excel', 'PowerPoint')

Write-Host 'Repairing Mukti registration for the current user...' -ForegroundColor Cyan

# 1) Locate the installed COM host DLL
$candidates = @(
    (Join-Path $env:LOCALAPPDATA 'Mukti\Mukti.WindowsAddin.comhost.dll'),
    (Join-Path $env:ProgramFiles  'Mukti\Mukti.WindowsAddin.comhost.dll'),
    (Join-Path ${env:ProgramFiles(x86)} 'Mukti\Mukti.WindowsAddin.comhost.dll')
) | Where-Object { $_ -and (Test-Path $_) }

if (-not $candidates) {
    Write-Host 'Could not find Mukti.WindowsAddin.comhost.dll. Please reinstall Mukti first.' -ForegroundColor Red
    Read-Host 'Press Enter to close'
    exit 1
}
$dll     = $candidates[0]
$AppPath = Split-Path $dll
Write-Host "Using: $dll"

# 2) Copy UCRT stub DLLs to the install directory if missing.
#    Microsoft 365 Click-to-Run's virtual file system does not expose
#    C:\Windows\System32\downlevel to COM DLLs hosted inside Office processes.
#    The .NET comhost imports api-ms-win-crt-*.dll from its own directory.
$ucrtDlls = @(
    'api-ms-win-crt-convert-l1-1-0.dll',
    'api-ms-win-crt-filesystem-l1-1-0.dll',
    'api-ms-win-crt-heap-l1-1-0.dll',
    'api-ms-win-crt-locale-l1-1-0.dll',
    'api-ms-win-crt-runtime-l1-1-0.dll',
    'api-ms-win-crt-stdio-l1-1-0.dll',
    'api-ms-win-crt-string-l1-1-0.dll',
    'api-ms-win-crt-time-l1-1-0.dll'
)
foreach ($ucrt in $ucrtDlls) {
    if (Test-Path (Join-Path $AppPath $ucrt)) { continue }
    foreach ($sysDir in @("$env:SystemRoot\System32\downlevel", "$env:SystemRoot\System32")) {
        $src = Join-Path $sysDir $ucrt
        if (Test-Path $src) {
            Copy-Item $src $AppPath -Force -EA SilentlyContinue
            Write-Host "  Copied UCRT stub: $ucrt"
            break
        }
    }
}

# 3) Write COM registration directly to HKCU (regsvr32 tries HKLM first and fails without elevation)
$clsidRoot = "HKCU:\SOFTWARE\Classes\CLSID\$Clsid"
New-Item -Path "$clsidRoot\InprocServer32" -Force | Out-Null
Set-ItemProperty "$clsidRoot\InprocServer32" '(Default)'      $dll
Set-ItemProperty "$clsidRoot\InprocServer32" 'ThreadingModel' 'Both'

$progIdRoot = "HKCU:\SOFTWARE\Classes\$ProgId"
New-Item -Path "$progIdRoot\CLSID" -Force | Out-Null
Set-ItemProperty "$progIdRoot\CLSID" '(Default)' $Clsid
Write-Host 'COM class registered in HKCU.'

# 4) Write Office add-in keys for this user (both versionless and 16.0 paths)
foreach ($app in $apps) {
    foreach ($ver in @('', '16.0\')) {
        $key = "HKCU:\SOFTWARE\Microsoft\Office\${ver}${app}\Addins\$ProgId"
        New-Item -Path $key -Force | Out-Null
        Set-ItemProperty $key 'FriendlyName'    'Mukti'
        Set-ItemProperty $key 'Description'     'Convert Bijoy/SutonnyMJ Bengali text to Unicode'
        Set-ItemProperty $key 'LoadBehavior'    3 -Type DWord
        Set-ItemProperty $key 'CommandLineSafe' 0 -Type DWord
    }
    Write-Host "Registered for $app."
}

Write-Host ''
Write-Host 'Done. Close Word/Excel/PowerPoint completely and reopen them.' -ForegroundColor Green
Write-Host 'You should see the Mukti tab on the ribbon.'                   -ForegroundColor Green
Read-Host 'Press Enter to close'
