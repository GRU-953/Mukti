param([switch]$Install, [switch]$Uninstall, [string]$AppPath = "")

$ProgId      = "Mukti.Connect"
$FriendlyName = "Mukti"
$Description  = "Convert Bijoy/SutonnyMJ Bengali text to Unicode"
$OfficeApps   = @("Word", "Excel", "PowerPoint")

if (-not $AppPath) {
    $AppPath = Split-Path -Parent $MyInvocation.MyCommand.Path
}

$DllPath = Join-Path $AppPath "Mukti.WindowsAddin.dll"

if ($Install) {
    if (-not (Test-Path $DllPath)) {
        Write-Host "ERROR: Mukti.WindowsAddin.dll not found at: $AppPath"
        Write-Host "Ensure the ARM64 binaries are installed before running this script."
        exit 1
    }

    Write-Host ""
    Write-Host "IMPORTANT: ARM64-native Microsoft Office is not yet available."
    Write-Host "Office currently runs as x64 under emulation on ARM64 devices."
    Write-Host "This registration will take effect when ARM64-native Office ships."
    Write-Host ""

    $ManifestPath = Join-Path $AppPath "Mukti.WindowsAddin.dll"

    foreach ($app in $OfficeApps) {
        $keyPath = "HKCU:\SOFTWARE\Microsoft\Office\$app\Addins\$ProgId"
        if (-not (Test-Path $keyPath)) { New-Item -Path $keyPath -Force | Out-Null }
        Set-ItemProperty -Path $keyPath -Name "FriendlyName"    -Value $FriendlyName
        Set-ItemProperty -Path $keyPath -Name "Description"     -Value $Description
        Set-ItemProperty -Path $keyPath -Name "LoadBehavior"    -Value 3 -Type DWord
        Set-ItemProperty -Path $keyPath -Name "CommandLineSafe" -Value 0 -Type DWord
        Set-ItemProperty -Path $keyPath -Name "Manifest"        -Value $ManifestPath
    }

    Write-Host "Mukti ARM64 registered for Office (HKCU)."
    Write-Host "To use Mukti today with x64-emulated Office, install the x64 build from:"
    Write-Host "  https://github.com/GRU-953/Mukti/releases"
}

if ($Uninstall) {
    foreach ($app in $OfficeApps) {
        $keyPath = "HKCU:\SOFTWARE\Microsoft\Office\$app\Addins\$ProgId"
        if (Test-Path $keyPath) { Remove-Item -Path $keyPath -Recurse -Force }
    }
    Write-Host "Mukti ARM64 unregistered from Office."
}
