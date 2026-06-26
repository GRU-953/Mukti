param([switch]$Install, [switch]$Uninstall, [string]$AppPath = "")

$ProgId    = "Mukti.Connect"
$Clsid     = "{F4E71C21-9B7A-4C3E-8D22-8F91A235C4B1}"
$FriendlyName = "Mukti"
$Description  = "Convert Bijoy/SutonnyMJ Bengali text to Unicode"
$OfficeApps   = @("Word", "Excel", "PowerPoint")

if ($Install) {
    # Resolve comhost path: use $AppPath if given, otherwise the script's own directory
    if ($AppPath -eq "") { $AppPath = $PSScriptRoot }
    $comhostDll = Join-Path $AppPath "Mukti.WindowsAddin.comhost.dll"

    # Write COM registration directly to HKCU (regsvr32 tries HKLM first and fails non-elevated)
    $clsidRoot = "HKCU:\SOFTWARE\Classes\CLSID\$Clsid"
    New-Item -Path "$clsidRoot"                    -Force | Out-Null
    New-Item -Path "$clsidRoot\InprocServer32"      -Force | Out-Null
    Set-ItemProperty "$clsidRoot\InprocServer32" "(Default)"     $comhostDll
    Set-ItemProperty "$clsidRoot\InprocServer32" "ThreadingModel" "Both"

    $progIdRoot = "HKCU:\SOFTWARE\Classes\$ProgId"
    New-Item -Path "$progIdRoot"        -Force | Out-Null
    New-Item -Path "$progIdRoot\CLSID"  -Force | Out-Null
    Set-ItemProperty "$progIdRoot\CLSID" "(Default)" $Clsid

    # Write Office Addin keys
    foreach ($app in $OfficeApps) {
        $keyPath = "HKCU:\SOFTWARE\Microsoft\Office\$app\Addins\$ProgId"
        New-Item -Path $keyPath -Force | Out-Null
        Set-ItemProperty $keyPath "FriendlyName"    $FriendlyName
        Set-ItemProperty $keyPath "Description"     $Description
        Set-ItemProperty $keyPath "LoadBehavior"    3 -Type DWord
        Set-ItemProperty $keyPath "CommandLineSafe" 0 -Type DWord
    }
    Write-Host "Mukti registered with Office"
}

if ($Uninstall) {
    # Remove COM registration
    $clsidRoot  = "HKCU:\SOFTWARE\Classes\CLSID\$Clsid"
    $progIdRoot = "HKCU:\SOFTWARE\Classes\$ProgId"
    if (Test-Path $clsidRoot)  { Remove-Item $clsidRoot  -Recurse -Force }
    if (Test-Path $progIdRoot) { Remove-Item $progIdRoot -Recurse -Force }

    # Remove Office Addin keys
    foreach ($app in $OfficeApps) {
        $keyPath = "HKCU:\SOFTWARE\Microsoft\Office\$app\Addins\$ProgId"
        if (Test-Path $keyPath) { Remove-Item $keyPath -Recurse -Force }
    }
    Write-Host "Mukti unregistered from Office"
}
