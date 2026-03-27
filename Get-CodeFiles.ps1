<#
Get-CodeFiles.ps1

Purpose
  Discovers source files in a project or across a whole solution, reports
  them to the console with colour, and either copies their names to the
  clipboard (for manual use with CopySource.ps1) or pipes directly into it.

Usage
  .\Get-CodeFiles.ps1 [options]

Examples
  .\Get-CodeFiles.ps1
  .\Get-CodeFiles.ps1 -Directory C:\Repos\MyApp
  .\Get-CodeFiles.ps1 -Project VaultCoach
  .\Get-CodeFiles.ps1 -Project *
  .\Get-CodeFiles.ps1 -Extensions cs,xaml,json
  .\Get-CodeFiles.ps1 -Pipe
  .\Get-CodeFiles.ps1 -Preview
  .\Get-CodeFiles.ps1 -h
#>

[CmdletBinding()]
param(
    [Alias("h")]
    [switch]$ShowHelp,

    [Parameter(Mandatory = $false)]
    [string]$Directory = ".",

    [Parameter(Mandatory = $false)]
    [string]$Project = "",

    [Parameter(Mandatory = $false)]
    [string[]]$Extensions = @("cs", "xaml", "json", "csproj"),

    [Parameter(Mandatory = $false)]
    [string[]]$AddExcludeDirs = @(),

    [switch]$Pipe,

    [Parameter(Mandatory = $false)]
    [string]$CopySourcePath = "",

    [Parameter(Mandatory = $false)]
    [int]$ChunkLimit = 100000,

    [switch]$Preview
)

if ($ShowHelp) {
    Write-Host ""
    Write-Host "Get-CodeFiles.ps1" -ForegroundColor Cyan
    Write-Host "-----------------" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "USAGE"
    Write-Host "  .\Get-CodeFiles.ps1 [options]"
    Write-Host ""
    Write-Host "OPTIONS"
    Write-Host "  -Directory <path>"
    Write-Host "      Root to search. Default: current directory (.)"
    Write-Host ""
    Write-Host "  -Project <name|*>"
    Write-Host "      Partial match against .csproj folder names."
    Write-Host "      Omit for interactive numbered menu (up to 9 projects)."
    Write-Host "      Pass * to merge all projects."
    Write-Host ""
    Write-Host "  -Extensions <ext,...>"
    Write-Host "      Extensions to include without leading dot."
    Write-Host "      Default: cs, xaml, json, csproj"
    Write-Host ""
    Write-Host "  -AddExcludeDirs <dir,...>"
    Write-Host "      Extra directory names to exclude."
    Write-Host "      Built-in: bin obj Debug Release .nuget wpftmp .git .vs .idea TestResults node_modules"
    Write-Host ""
    Write-Host "  -Pipe"
    Write-Host "      Invoke CopySource.ps1 automatically."
    Write-Host ""
    Write-Host "  -CopySourcePath <path>"
    Write-Host "      Path to CopySource.ps1. Default: same directory as this script."
    Write-Host ""
    Write-Host "  -ChunkLimit <int>"
    Write-Host "      Passed to CopySource.ps1 when -Pipe is used. Default: 100000"
    Write-Host ""
    Write-Host "  -Preview"
    Write-Host "      Report discovered files without touching the clipboard."
    Write-Host ""
    Write-Host "  -h | -ShowHelp"
    Write-Host "      Show this help and exit."
    Write-Host ""
    Write-Host "NOTES"
    Write-Host "  Always excluded: appsettings.*.json launchSettings.json *.Designer.cs *.g.cs AssemblyInfo.cs"
    Write-Host ""
    exit 0
}

$ErrorActionPreference = "Stop"

$builtInExcludeDirs = @(
    "bin", "obj", "Debug", "Release", ".nuget", "wpftmp",
    ".git", ".vs", ".idea", "TestResults", "node_modules"
)
$allExcludeDirs = $builtInExcludeDirs + $AddExcludeDirs

$excludeNamePatterns = @(
    "\.g(\.i)?\.cs$",
    "\.Designer\.cs$",
    "AssemblyInfo\.cs$",
    "^appsettings\..+\.json$",
    "^launchSettings\.json$"
)

function Test-UnderExcludedDir {
    param([string]$FullPath)
    $parts = $FullPath.Split([IO.Path]::DirectorySeparatorChar)
    foreach ($p in $parts) {
        if ($allExcludeDirs -contains $p) { return $true }
    }
    return $false
}

function Test-ExcludedByName {
    param([string]$Name)
    foreach ($pat in $excludeNamePatterns) {
        if ($Name -match $pat) { return $true }
    }
    return $false
}

function Get-FilesUnder {
    param([string]$SearchRoot)
    $found = [System.Collections.Generic.List[System.IO.FileInfo]]::new()
    foreach ($ext in $Extensions) {
        $e = $ext.TrimStart(".")
        Get-ChildItem -Path $SearchRoot -Recurse -Filter "*.$e" -File -Force -ErrorAction SilentlyContinue |
            Where-Object {
                -not (Test-UnderExcludedDir -FullPath $_.FullName) -and
                -not (Test-ExcludedByName   -Name     $_.Name)
            } |
            ForEach-Object { $found.Add($_) }
    }
    return ($found | Sort-Object FullName)
}

function Show-FileList {
    param(
        [System.IO.FileInfo[]]$Files,
        [string]$DisplayRoot,
        [string]$Label = "FILES"
    )
    $nameGroups = $Files | Group-Object -Property Name | Where-Object { $_.Count -gt 1 }
    $dupNames   = @{}
    foreach ($g in $nameGroups) { $dupNames[$g.Name] = $true }

    Write-Host ""
    $count = $Files.Count
    $suffix = if ($count -ne 1) { "s" } else { "" }
    Write-Host "$Label ($count file$suffix):" -ForegroundColor Cyan
    Write-Host ""

    foreach ($f in $Files) {
        if ($dupNames.ContainsKey($f.Name)) {
            $rel = $f.FullName.Substring($DisplayRoot.Length).TrimStart("\", "/")
            Write-Host "  [DUP] $rel" -ForegroundColor Yellow
        }
        else {
            Write-Host "  $($f.Name)" -ForegroundColor White
        }
    }

    Write-Host ""
    if ($dupNames.Count -gt 0) {
        $dc = $dupNames.Count
        Write-Host "  $dc duplicate name(s) shown with repo-relative path." -ForegroundColor Yellow
        Write-Host ""
    }
    return $dupNames
}

if (-not (Test-Path -LiteralPath $Directory -PathType Container)) {
    Write-Host "ERROR: Directory not found: $Directory" -ForegroundColor Red
    exit 1
}
$root = (Resolve-Path -LiteralPath $Directory).Path

Write-Host ""
Write-Host "Solution root : $root" -ForegroundColor DarkCyan
Write-Host "Extensions    : $($Extensions -join ", ")" -ForegroundColor DarkGray
if ($AddExcludeDirs.Count -gt 0) {
    Write-Host "Extra exclude : $($AddExcludeDirs -join ", ")" -ForegroundColor DarkGray
}

$csprojFiles = @(
    Get-ChildItem -Path $root -Recurse -Filter "*.csproj" -File -Force -ErrorAction SilentlyContinue |
        Where-Object { -not (Test-UnderExcludedDir -FullPath $_.FullName) } |
        Sort-Object FullName
)

$searchRoots = @()

if ($csprojFiles.Count -eq 0) {
    Write-Host ""
    Write-Host "No .csproj files found - searching entire directory." -ForegroundColor Yellow
    $searchRoots = @($root)
}
elseif ($Project -eq "*") {
    $searchRoots = $csprojFiles | ForEach-Object { $_.DirectoryName } | Select-Object -Unique
    Write-Host ""
    Write-Host "Mode: ALL PROJECTS ($($searchRoots.Count))" -ForegroundColor Cyan
}
elseif ($Project -ne "") {
    $matched = @($csprojFiles | Where-Object { $_.BaseName -ilike "*$Project*" })
    if ($matched.Count -eq 0) {
        Write-Host ""
        Write-Host "ERROR: No .csproj matching: $Project" -ForegroundColor Red
        exit 1
    }
    if ($matched.Count -gt 1) {
        Write-Host ""
        Write-Host "WARNING: Multiple matches for $Project" -ForegroundColor Yellow
        foreach ($m in $matched) { Write-Host "  $($m.FullName)" -ForegroundColor Yellow }
        Write-Host "Using first: $($matched[0].FullName)" -ForegroundColor Yellow
    }
    $searchRoots = @($matched[0].DirectoryName)
    Write-Host ""
    Write-Host "Project : $($matched[0].BaseName)  ($($searchRoots[0]))" -ForegroundColor Cyan
}
else {
    if ($csprojFiles.Count -gt 9) {
        Write-Host ""
        Write-Host "WARNING: $($csprojFiles.Count) projects found; menu shows first 9." -ForegroundColor Yellow
        Write-Host "Use -Project <n> or -Project * to target others." -ForegroundColor Yellow
    }
    $menuProjects = $csprojFiles | Select-Object -First 9
    Write-Host ""
    Write-Host "Select a project:" -ForegroundColor Cyan
    Write-Host ""
    for ($i = 0; $i -lt $menuProjects.Count; $i++) {
        $num = $i + 1
        $rel = $menuProjects[$i].FullName.Substring($root.Length).TrimStart("\", "/")
        Write-Host "  [$num] $($menuProjects[$i].BaseName)   $rel" -ForegroundColor White
    }
    $showAll = $menuProjects.Count -gt 1
    if ($showAll) { Write-Host "  [A] All projects" -ForegroundColor White }
    Write-Host ""
    $validKeys = 1..$menuProjects.Count | ForEach-Object { "$_" }
    if ($showAll) { $validKeys += "A" }
    Write-Host "Press a key ($($validKeys -join ", "))..." -ForegroundColor DarkGray
    $choice = $null
    while ($null -eq $choice) {
        $key = [Console]::ReadKey($true)
        $ch  = $key.KeyChar.ToString().ToUpper()
        if ($showAll -and $ch -eq "A") { $choice = "A" }
        elseif ($ch -match "^\d$") {
            $n = [int]$ch
            if ($n -ge 1 -and $n -le $menuProjects.Count) { $choice = $n }
        }
    }
    if ($choice -eq "A") {
        $searchRoots = $menuProjects | ForEach-Object { $_.DirectoryName } | Select-Object -Unique
        Write-Host "-> All projects ($($searchRoots.Count))" -ForegroundColor Cyan
    }
    else {
        $sel = $menuProjects[$choice - 1]
        $searchRoots = @($sel.DirectoryName)
        Write-Host "-> $($sel.BaseName)" -ForegroundColor Cyan
    }
}

Write-Host ""
Write-Host "Scanning..." -ForegroundColor DarkGray

$allFiles = [System.Collections.Generic.List[System.IO.FileInfo]]::new()
foreach ($sr in $searchRoots) {
    foreach ($f in (Get-FilesUnder -SearchRoot $sr)) { $allFiles.Add($f) }
}
$allFiles = @($allFiles | Sort-Object FullName -Unique)

if ($allFiles.Count -eq 0) {
    Write-Host "No matching files found." -ForegroundColor Red
    exit 0
}

$displayRoot = if ($searchRoots.Count -gt 1) { $root } else { $searchRoots[0] }

$label    = if ($Preview) { "PREVIEW" } else { "FILES" }
$dupNames = Show-FileList -Files $allFiles -DisplayRoot $displayRoot -Label $label

$totalChars = ($allFiles | ForEach-Object {
    try { (Get-Content -LiteralPath $_.FullName -Raw -ErrorAction Stop).Length } catch { 0 }
} | Measure-Object -Sum).Sum

$estChunks = [Math]::Ceiling($totalChars / $ChunkLimit)
$suffix    = if ($estChunks -ne 1) { "s" } else { "" }
Write-Host "Estimated content : ~$totalChars chars  ->  ~$estChunks chunk$suffix at limit $ChunkLimit" -ForegroundColor DarkGray
Write-Host ""

if ($Preview) {
    Write-Host "Preview only - clipboard unchanged." -ForegroundColor DarkGray
    exit 0
}

$fileArgs = $allFiles | ForEach-Object {
    if ($dupNames.ContainsKey($_.Name)) {
        $_.FullName.Substring($displayRoot.Length).TrimStart("\", "/")
    }
    else {
        $_.Name
    }
}

if ($Pipe) {
    if ($CopySourcePath -eq "") {
        $CopySourcePath = Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) "CopySource.ps1"
    }
    if (-not (Test-Path -LiteralPath $CopySourcePath)) {
        Write-Host "ERROR: CopySource.ps1 not found at: $CopySourcePath" -ForegroundColor Red
        Write-Host "Use -CopySourcePath to specify its location." -ForegroundColor Red
        exit 1
    }
    Write-Host "Piping into CopySource.ps1 (ChunkLimit: $ChunkLimit)..." -ForegroundColor Cyan
    Write-Host ""
    & $CopySourcePath -RootPath $displayRoot -ChunkLimit $ChunkLimit @fileArgs
    exit 0
}

($fileArgs -join " ") | Set-Clipboard

$n = $allFiles.Count
$s = if ($n -ne 1) { "s" } else { "" }
Write-Host "Copied $n filename$s to clipboard." -ForegroundColor Green
Write-Host "Re-run with -Pipe to invoke CopySource.ps1 directly." -ForegroundColor DarkGray
Write-Host ""