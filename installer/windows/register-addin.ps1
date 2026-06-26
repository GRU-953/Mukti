param([switch]$Install, [switch]$Uninstall, [string]$AppPath = "")

$ProgId    = "Mukti.Connect"
$Clsid     = "{F4E71C21-9B7A-4C3E-8D22-8F91A235C4B1}"
$FriendlyName = "Mukti"
$Description  = "Convert Bijoy/SutonnyMJ Bengali text to Unicode"
$OfficeApps   = @("Word", "Excel", "PowerPoint")

if ($Install) {
    # The install directory is always the canonical LOCALAPPDATA location.
    # $AppPath (defaults to $PSScriptRoot) is only used below as the *source*
    # for UCRT stub copies — it must NOT be used for the COM registry path.
    # HKCU takes priority over HKLM; relative paths resolve from Office's working
    # directory (C:\Program Files\Microsoft Office\root\Office16\), not the install
    # dir, so the DLL would never be found. Always write the full absolute path.
    $installDir = Join-Path $env:LOCALAPPDATA "Mukti"
    $comdll     = Join-Path $installDir "Mukti.WindowsAddin.comhost.dll"
    if ($AppPath -eq "") { $AppPath = $PSScriptRoot }

    # Microsoft 365 Click-to-Run runs Office in a virtual file system that does not
    # expose C:\Windows\System32\downlevel to COM DLLs hosted inside Office processes.
    # The .NET comhost imports api-ms-win-crt-*.dll (UCRT stubs). Copy them from the
    # system if they are not already present in the install directory.
    $ucrtDlls = @(
        "api-ms-win-crt-convert-l1-1-0.dll",
        "api-ms-win-crt-filesystem-l1-1-0.dll",
        "api-ms-win-crt-heap-l1-1-0.dll",
        "api-ms-win-crt-locale-l1-1-0.dll",
        "api-ms-win-crt-runtime-l1-1-0.dll",
        "api-ms-win-crt-stdio-l1-1-0.dll",
        "api-ms-win-crt-string-l1-1-0.dll",
        "api-ms-win-crt-time-l1-1-0.dll"
    )
    foreach ($dll in $ucrtDlls) {
        if (Test-Path (Join-Path $AppPath $dll)) { continue }
        foreach ($sysDir in @("$env:SystemRoot\System32\downlevel", "$env:SystemRoot\System32")) {
            $src = Join-Path $sysDir $dll
            if (Test-Path $src) {
                Copy-Item $src $AppPath -Force -EA SilentlyContinue
                break
            }
        }
    }

    # Write COM registration directly to HKCU (regsvr32 tries HKLM first and fails non-elevated)
    $clsidRoot = "HKCU:\SOFTWARE\Classes\CLSID\$Clsid"
    New-Item -Path "$clsidRoot"                                                       -Force | Out-Null
    New-Item -Path "$clsidRoot\InprocServer32"                                        -Force | Out-Null
    Set-ItemProperty -Path "$clsidRoot\InprocServer32" -Name "(default)"       -Value $comdll
    Set-ItemProperty -Path "$clsidRoot\InprocServer32" -Name "ThreadingModel"  -Value "Both"

    $progIdRoot = "HKCU:\SOFTWARE\Classes\$ProgId"
    New-Item -Path "$progIdRoot"        -Force | Out-Null
    New-Item -Path "$progIdRoot\CLSID"  -Force | Out-Null
    Set-ItemProperty "$progIdRoot\CLSID" "(Default)" $Clsid

    # Write Office Addin keys to both paths:
    #   versionless:  Office\{App}\Addins  (read by all Office versions)
    #   versioned:    Office\16.0\{App}\Addins  (read by Office 2016/2019/2021/365)
    foreach ($app in $OfficeApps) {
        foreach ($ver in @("", "16.0\")) {
            $keyPath = "HKCU:\SOFTWARE\Microsoft\Office\${ver}${app}\Addins\$ProgId"
            New-Item -Path $keyPath -Force | Out-Null
            Set-ItemProperty $keyPath "FriendlyName"    $FriendlyName
            Set-ItemProperty $keyPath "Description"     $Description
            Set-ItemProperty $keyPath "LoadBehavior"    3 -Type DWord
            Set-ItemProperty $keyPath "CommandLineSafe" 0 -Type DWord
        }
    }
    Write-Host "Mukti registered with Office"
}

if ($Uninstall) {
    # Remove COM registration
    $clsidRoot  = "HKCU:\SOFTWARE\Classes\CLSID\$Clsid"
    $progIdRoot = "HKCU:\SOFTWARE\Classes\$ProgId"
    if (Test-Path $clsidRoot)  { Remove-Item $clsidRoot  -Recurse -Force }
    if (Test-Path $progIdRoot) { Remove-Item $progIdRoot -Recurse -Force }

    # Remove Office Addin keys from both paths
    foreach ($app in $OfficeApps) {
        foreach ($ver in @("", "16.0\")) {
            $keyPath = "HKCU:\SOFTWARE\Microsoft\Office\${ver}${app}\Addins\$ProgId"
            if (Test-Path $keyPath) { Remove-Item $keyPath -Recurse -Force }
        }
    }
    Write-Host "Mukti unregistered from Office"
}
