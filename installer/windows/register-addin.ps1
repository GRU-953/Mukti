param([switch]$Install, [switch]$Uninstall, [string]$AppPath = "")

$ProgId = "Mukti.Connect"
$FriendlyName = "Mukti"
$Description = "Convert Bijoy/SutonnyMJ Bengali text to Unicode"
$OfficeApps = @("Word", "Excel", "PowerPoint")

if ($Install) {
    foreach ($app in $OfficeApps) {
        $keyPath = "HKCU:\\SOFTWARE\\Microsoft\\Office\\$app\\Addins\\$ProgId"
        if (-not (Test-Path $keyPath)) { New-Item -Path $keyPath -Force | Out-Null }
        Set-ItemProperty -Path $keyPath -Name "FriendlyName" -Value $FriendlyName
        Set-ItemProperty -Path $keyPath -Name "Description" -Value $Description
        Set-ItemProperty -Path $keyPath -Name "LoadBehavior" -Value 3 -Type DWord
        Set-ItemProperty -Path $keyPath -Name "CommandLineSafe" -Value 0 -Type DWord
    }
    Write-Host "Mukti registered with Office"
}

if ($Uninstall) {
    foreach ($app in $OfficeApps) {
        $keyPath = "HKCU:\\SOFTWARE\\Microsoft\\Office\\$app\\Addins\\$ProgId"
        if (Test-Path $keyPath) { Remove-Item -Path $keyPath -Recurse -Force }
    }
    Write-Host "Mukti unregistered from Office"
}
