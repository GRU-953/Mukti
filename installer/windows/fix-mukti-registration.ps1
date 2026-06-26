# fix-mukti-registration.ps1
# Repairs Mukti when it was installed but does not appear in Word/Excel/PowerPoint.
#
# Cause: an older Mukti installer elevated to administrator and registered the
# add-in in the administrator account's registry, not yours. This script
# re-registers Mukti in YOUR account. Run it as yourself — do NOT "Run as
# administrator" (that would recreate the original problem).
#
# How to run: right-click this file -> "Run with PowerShell".

$ErrorActionPreference = 'Stop'
$ProgId = 'Mukti.Connect'
$apps   = @('Word', 'Excel', 'PowerPoint')

Write-Host 'Repairing Mukti registration for the current user...' -ForegroundColor Cyan

# 1) Locate the installed COM host DLL (per-user or older per-machine location).
$candidates = @(
    (Join-Path $env:LOCALAPPDATA 'Mukti\Mukti.WindowsAddin.comhost.dll'),
    (Join-Path $env:ProgramFiles 'Mukti\Mukti.WindowsAddin.comhost.dll'),
    (Join-Path ${env:ProgramFiles(x86)} 'Mukti\Mukti.WindowsAddin.comhost.dll')
) | Where-Object { $_ -and (Test-Path $_) }

if (-not $candidates) {
    Write-Host 'Could not find Mukti.WindowsAddin.comhost.dll. Please reinstall Mukti first.' -ForegroundColor Red
    Read-Host 'Press Enter to close'
    exit 1
}

# Always use the 64-bit regsvr32 for the x64 add-in. If this happens to run in a
# 32-bit PowerShell on 64-bit Windows, System32 is redirected to SysWOW64, so use
# the Sysnative alias to reach the real 64-bit regsvr32.
if ([Environment]::Is64BitOperatingSystem -and -not [Environment]::Is64BitProcess) {
    $regsvr = "$env:SystemRoot\Sysnative\regsvr32.exe"
} else {
    $regsvr = "$env:SystemRoot\System32\regsvr32.exe"
}

# If a stale all-users copy is left in Program Files, unregister it first so it
# cannot shadow the per-user registration we are about to create.
$prefer = ($candidates | Where-Object { $_ -like "$env:LOCALAPPDATA*" } | Select-Object -First 1)
if ($prefer) {
    foreach ($stale in ($candidates | Where-Object { $_ -ne $prefer })) {
        Write-Host "Unregistering stale copy: $stale"
        & $regsvr /s /u "$stale"
    }
    $dll = $prefer
} else {
    $dll = $candidates[0]
}
Write-Host "Using: $dll"

# 2) Register the COM class in this user's hive (no admin needed).
& $regsvr /s "$dll"
Write-Host 'COM class registered for current user.'

# 3) Tell Office to load the add-in, for this user.
foreach ($app in $apps) {
    $key = "HKCU:\SOFTWARE\Microsoft\Office\$app\Addins\$ProgId"
    if (-not (Test-Path $key)) { New-Item -Path $key -Force | Out-Null }
    Set-ItemProperty -Path $key -Name 'FriendlyName'    -Value 'Mukti'
    Set-ItemProperty -Path $key -Name 'Description'     -Value 'Convert Bijoy/SutonnyMJ Bengali text to Unicode'
    Set-ItemProperty -Path $key -Name 'LoadBehavior'    -Value 3 -Type DWord
    Set-ItemProperty -Path $key -Name 'CommandLineSafe' -Value 0 -Type DWord
    Write-Host "Registered for $app."
}

Write-Host ''
Write-Host 'Done. Close Word/Excel/PowerPoint completely and reopen them.' -ForegroundColor Green
Write-Host 'You should now see the Mukti tab on the ribbon.' -ForegroundColor Green
Read-Host 'Press Enter to close'
