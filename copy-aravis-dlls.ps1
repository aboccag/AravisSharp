# copy-aravis-dlls.ps1
# Copie libaravis + toutes les dépendances non-système (transitif) dans un dossier runtimes/win-x64/native
# Requiert MSYS2 (mingw64) installé avec ldd disponible (via pacman -S mingw-w64-x86_64-toolchain ou via aravis deps).

param(
    [string]$MsysRoot = "",
    [string]$DestRoot = "C:\dev\projects\abo\AravisSharpWindows",
    [ValidateSet("mingw64","ucrt64","clang64")]
    [string]$Env = "mingw64",
    [string]$AravisDllName = "libaravis-0.8-0.dll"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Resolve-MsysRoot {
    param([string]$Provided)

    if ($Provided -and (Test-Path $Provided)) { return (Resolve-Path $Provided).Path }

    $candidates = @(
    @(
        "C:\msys64",
        "$env:ProgramFiles\msys64",
        "$env:ProgramFiles(x86)\msys64"
    ) | Where-Object { $_ -and (Test-Path $_) }
)

if ($candidates.Length -eq 0) {
        throw "MSYS2 root introuvable. Passe -MsysRoot 'C:\msys64' (ou autre chemin)."
    }
    return (Resolve-Path $candidates[0]).Path
}

function Get-MsysBash {
    param([string]$Root)
    $bash = Join-Path $Root "usr\bin\bash.exe"
    if (!(Test-Path $bash)) { throw "bash.exe introuvable dans $bash" }
    return $bash
}

function Get-EnvBinPath {
    param([string]$Root, [string]$EnvName)

    switch ($EnvName) {
        "mingw64" { return (Join-Path $Root "mingw64\bin") }
        "ucrt64"  { return (Join-Path $Root "ucrt64\bin") }
        "clang64" { return (Join-Path $Root "clang64\bin") }
    }
}

function Convert-ToMsysPath {
    # convertit C:\msys64\mingw64\bin -> /c/msys64/mingw64/bin
    param([string]$WinPath)
    $p = $WinPath -replace "\\","/"
    if ($p -match "^([A-Za-z]):/(.*)$") {
        $drive = $matches[1].ToLower()
        $rest  = $matches[2]
        return "/$drive/$rest"
    }
    return $p
}

function Invoke-Ldd {
    param(
        [string]$BashExe,
        [string]$MsysBinPathWin,
        [string]$TargetDllWin
    )

    $msysBinPath = Convert-ToMsysPath $MsysBinPathWin
    $target      = Convert-ToMsysPath $TargetDllWin

    $cmd = "export PATH=`"$msysBinPath`":`$PATH; ldd `"$target`""
    & $BashExe -lc $cmd
}

function Parse-LddOutput {
    param([string[]]$Lines)

    $deps = New-Object System.Collections.Generic.HashSet[string]

    foreach ($line in $Lines) {
        # exemples:
        #  libglib-2.0-0.dll => /mingw64/bin/libglib-2.0-0.dll (0x...)
        #  KERNEL32.DLL => /c/WINDOWS/System32/KERNEL32.DLL (0x...)
        if ($line -match "=>\s+([^\s]+)\s+\(") {
            $path = $matches[1].Trim()
            # Ignore les dll système windows (case-insensitive)
            if ($path -match "^/c/windows" -or $path -match "^/c/WINDOWS") { continue }
            # Ignore les "not found"
            if ($path -match "not found") { continue }

            # on garde tout ce qui n'est pas système: /mingw64/bin/..., /ucrt64/bin/..., etc
            $deps.Add($path) | Out-Null
        }
    }

    return $deps
}

function Convert-MsysToWindowsPath {
    param([string]$MsysRootWin, [string]$MsysPath)

    # /mingw64/bin/foo.dll -> C:\msys64\mingw64\bin\foo.dll
    # /c/windows/... -> C:\windows\...
    $p = $MsysPath.Trim()

    if ($p -match "^/([a-z])/(.*)$") {
        $drive = $matches[1].ToUpper()
        $rest  = $matches[2] -replace "/", "\"
        return "$drive`:\$rest"
    }

    if ($p.StartsWith("/")) {
        $rest = $p.Substring(1) -replace "/", "\"
        return (Join-Path $MsysRootWin $rest)
    }

    return $p
}

function Get-TransitiveDeps {
    param(
        [string]$BashExe,
        [string]$MsysRootWin,
        [string]$EnvBinWin,
        [string]$RootDllWin
    )

    $toProcess = New-Object System.Collections.Generic.Queue[string]
    $seenWin   = New-Object System.Collections.Generic.HashSet[string]
    $finalWin  = New-Object System.Collections.Generic.HashSet[string]

    $toProcess.Enqueue($RootDllWin)
    $seenWin.Add((Resolve-Path $RootDllWin).Path) | Out-Null

    while ($toProcess.Count -gt 0) {
        $cur = $toProcess.Dequeue()
        $finalWin.Add((Resolve-Path $cur).Path) | Out-Null

        $out = Invoke-Ldd -BashExe $BashExe -MsysBinPathWin $EnvBinWin -TargetDllWin $cur
        $lines = $out -split "`r?`n" | Where-Object { $_ -and $_.Trim().Length -gt 0 }

        $depsMsys = Parse-LddOutput -Lines $lines

        foreach ($d in $depsMsys) {
            $win = Convert-MsysToWindowsPath -MsysRootWin $MsysRootWin -MsysPath $d
            if (!(Test-Path $win)) {
                throw "Dépendance référencée mais introuvable sur disque: $d -> $win"
            }

            $resolved = (Resolve-Path $win).Path
            if (!$seenWin.Contains($resolved)) {
                $seenWin.Add($resolved) | Out-Null
                $toProcess.Enqueue($resolved)
            }
        }
    }

    return $finalWin
}

# --- Main ---
$MsysRootResolved = Resolve-MsysRoot -Provided $MsysRoot
$BashExe          = Get-MsysBash -Root $MsysRootResolved
$EnvBinWin        = Get-EnvBinPath -Root $MsysRootResolved -EnvName $Env

if (!(Test-Path $EnvBinWin)) { throw "Env bin path introuvable: $EnvBinWin" }

$RootDllWin = Join-Path $EnvBinWin $AravisDllName
if (!(Test-Path $RootDllWin)) {
    throw "DLL Aravis introuvable: $RootDllWin. Vérifie l'environnement -Env et le nom -AravisDllName."
}

$destNative = Join-Path $DestRoot "runtimes\win-x64\native"
New-Item -ItemType Directory -Force -Path $destNative | Out-Null

Write-Host "MSYS2 root : $MsysRootResolved"
Write-Host "Env bin    : $EnvBinWin"
Write-Host "Root DLL   : $RootDllWin"
Write-Host "Dest       : $destNative"
Write-Host ""

$deps = Get-TransitiveDeps -BashExe $BashExe -MsysRootWin $MsysRootResolved -EnvBinWin $EnvBinWin -RootDllWin $RootDllWin

Write-Host "Copie des DLL non-système (transitif) : $($deps.Count) fichiers"
foreach ($dll in ($deps | Sort-Object)) {
    $name = Split-Path $dll -Leaf
    Copy-Item -Force -Path $dll -Destination (Join-Path $destNative $name)
    Write-Host "  + $name"
}

# Crée un .nuspec minimal pour NuGet Package Explorer
$nuspecPath = Join-Path $DestRoot "AravisSharp.runtime.win-x64.nuspec"
$nuspec = @"
<?xml version="1.0"?>
<package>
  <metadata>
    <id>AravisSharp.runtime.win-x64</id>
    <version>0.8.33.0</version>
    <authors>AravisSharp</authors>
    <description>Native Aravis (and dependencies) for Windows x64, packaged for AravisSharp.</description>
    <requireLicenseAcceptance>false</requireLicenseAcceptance>
    <tags>aravis genicam gigE usb3vision vision</tags>
  </metadata>
</package>
"@
Set-Content -Path $nuspecPath -Value $nuspec -Encoding UTF8

Write-Host ""
Write-Host "OK. DLL copiées dans: $destNative"
Write-Host "Nuspec créé: $nuspecPath"
Write-Host "Ensuite: ouvre $nuspec dans NuGet Package Explorer, ajoute le dossier runtimes/, puis sauvegarde le .nupkg."
