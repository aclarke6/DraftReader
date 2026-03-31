<# 
Copy-SourceFiles.ps1

Purpose
  Copies the contents of source files (found by filename) into the clipboard,
  preceded by a header containing the file name, repo-relative path, and full path.

Usage
  .\Copy-SourceFiles.ps1 <file1> <file2> ... [options]
  .\Copy-SourceFiles.ps1 -Names <file1,file2,...> [options]
  .\Copy-SourceFiles.ps1 -Files <file1,file2,...> [options]

Examples
  .\Copy-SourceFiles.ps1 Program.cs VaultReader.cs
  .\Copy-SourceFiles.ps1 -Names Program.cs VaultReader.cs
  .\Copy-SourceFiles.ps1 -Files Program.cs VaultReader.cs
  .\Copy-SourceFiles.ps1 -RootPath C:\Users\alast\source\repos\VaultCoach Program.cs
  .\Copy-SourceFiles.ps1 -RootPath . -OutputFile Dump.txt Program.cs VaultReader.cs
  .\Copy-SourceFiles.ps1 -h
#>

[CmdletBinding()]
param(
    # Show help and exit
    [Alias("h")]
    [switch]$ShowHelp,

    # Accept filenames positionally OR via -Names / -Files
    [Parameter(Position = 0, ValueFromRemainingArguments = $true)]
    [Alias("Names", "Files")]
    [string[]]$FileNames,

    # Repo root (defaults to current directory)
    [Parameter(Mandatory = $false)]
    [string]$RootPath = ".",

    # If provided, also write the dump to a file (UTF-8)
    [Parameter(Mandatory = $false)]
    [string]$OutputFile = "",

    # Maximum characters per clipboard chunk
    [Parameter(Mandatory = $false)]
    [int]$ChunkLimit = 100000
)

if ($ShowHelp -or -not $FileNames -or @($FileNames).Count -eq 0) {

    Write-Host @"
Copy-SourceFiles.ps1
-------------------
Copies the contents of source files (found by filename) into the clipboard,
preceded by a header containing the file name, repo-relative path, and full path.

USAGE
  .\Copy-SourceFiles.ps1 <file1> <file2> ... [options]
  .\Copy-SourceFiles.ps1 -Names <file1,file2,...> [options]
  .\Copy-SourceFiles.ps1 -Files <file1,file2,...> [options]

OPTIONS
  -Names | -Files | (positional)
      One or more filenames only (no paths).
      Example: Program.cs VaultReader.cs

  -RootPath <path>
      Root directory to search from.
      Default: current directory (.)

  -OutputFile <path>
      If provided, also writes the output to a file (UTF-8).

  -ChunkLimit <int>
      Maximum characters per clipboard chunk.
      Default: 100000

  -h | -ShowHelp
      Show this help text and exit.

NOTES
  If you use positional filenames, put -RootPath, -OutputFile and -ChunkLimit BEFORE the filenames:
    .\Copy-SourceFiles.ps1 -RootPath . -OutputFile Dump.txt Program.cs VaultReader.cs
"@
    exit 0
}

$ErrorActionPreference = "Stop"

$excludeDirs = @(
    "bin",
    "obj",
    ".git",
    ".vs",
    ".idea",
    "TestResults",
    "node_modules"
)

if (-not (Test-Path -LiteralPath $RootPath -PathType Container)) {
    throw "RootPath '$RootPath' is not a directory. Pass -RootPath explicitly, e.g. -RootPath 'C:\Users\alast\source\repos\VaultCoach'."
}

$root = (Resolve-Path -LiteralPath $RootPath).Path

function Get-RepoRelativePath {
    param(
        [Parameter(Mandatory = $true)][string]$FullPath,
        [Parameter(Mandatory = $true)][string]$RootPathResolved
    )

    $rootWithSep = $RootPathResolved.TrimEnd('\') + '\'
    if ($FullPath.StartsWith($rootWithSep, [System.StringComparison]::OrdinalIgnoreCase)) {
        return $FullPath.Substring($rootWithSep.Length)
    }
    return $FullPath
}

function Is-UnderExcludedDir {
    param(
        [Parameter(Mandatory = $true)][string]$FullPath
    )

    $parts = $FullPath.Split([IO.Path]::DirectorySeparatorChar)
    foreach ($p in $parts) {
        if ($excludeDirs -contains $p) { return $true }
    }
    return $false
}

function Find-FileByName {
    param(
        [Parameter(Mandatory = $true)][string]$FileName
    )

    Get-ChildItem -Path $root -Recurse -File -Force -ErrorAction SilentlyContinue |
        Where-Object {
            $_.Name -ieq $FileName -and
            -not (Is-UnderExcludedDir -FullPath $_.FullName)
        } |
        Sort-Object FullName
}

$currentChunk      = New-Object System.Text.StringBuilder
$script:chunkIndex = 1
# Track which files have been fully flushed to the clipboard
$script:filesInChunk = [System.Collections.Generic.List[string]]::new()

function Emit-Chunk {
    param(
        [string]$Text,
        [bool]$IsFinal = $false,
        [string[]]$FilesIncluded = @()
    )

    if ([string]::IsNullOrWhiteSpace($Text)) { return }

    $wrapped = New-Object System.Text.StringBuilder

    if ($IsFinal) {
        [void]$wrapped.AppendLine("<<< FINAL CHUNK $script:chunkIndex — all content is included >>>")
    }
    else {
        [void]$wrapped.AppendLine("<<< CHUNK $script:chunkIndex — more chunks will follow >>>")
    }

    [void]$wrapped.AppendLine("")
    [void]$wrapped.AppendLine($Text)
    [void]$wrapped.AppendLine("")

    if ($IsFinal) {
        [void]$wrapped.AppendLine("<<< END OF FINAL CHUNK — nothing further will follow >>>")
    }
    else {
        [void]$wrapped.AppendLine("<<< END OF CHUNK $script:chunkIndex — Wait for more chunks! >>>")
    }

    $finalText = $wrapped.ToString()

    Write-Host ""

    if ($IsFinal) {
        Write-Host "=== FINAL CHUNK #$script:chunkIndex ===" -ForegroundColor Green
    }
    else {
        Write-Host "=== Chunk #$script:chunkIndex ===" -ForegroundColor Cyan
    }

    Write-Host "Characters : $($finalText.Length)" -ForegroundColor DarkGray

    if ($FilesIncluded.Count -gt 0) {
        Write-Host "Files clipped in this chunk:" -ForegroundColor Yellow
        foreach ($f in $FilesIncluded) {
            Write-Host "  + $f" -ForegroundColor Yellow
        }
    }

    $finalText | Set-Clipboard

    if ($OutputFile -ne "") {
        $outPath = [IO.Path]::ChangeExtension($OutputFile, ".$script:chunkIndex.txt")
        $finalText | Out-File -FilePath $outPath -Encoding utf8
        Write-Host "Also wrote : $outPath" -ForegroundColor DarkGray
    }

    if ($IsFinal) {
        Write-Host ""
        Write-Host "All done — final chunk is in the clipboard." -ForegroundColor Green
        Write-Host ""
    }
    else {
        Write-Host ""
        Write-Host "Copied to clipboard. Press ENTER for next chunk..." -ForegroundColor Cyan
        Write-Host ""
        $null = Read-Host
        $script:chunkIndex++
    }
}

foreach ($name in $FileNames) {

    $fileBlock = New-Object System.Text.StringBuilder
    $fileMatches = @(Find-FileByName -FileName $name)

    if (-not $fileMatches -or @($fileMatches).Count -eq 0) {
        Write-Host "  NOT FOUND : $name" -ForegroundColor Red
        [void]$fileBlock.AppendLine("")
        [void]$fileBlock.AppendLine("============================================================")
        [void]$fileBlock.AppendLine("FILE: $name")
        [void]$fileBlock.AppendLine("STATUS: MISSING (not found under $root)")
        [void]$fileBlock.AppendLine("============================================================")
        [void]$fileBlock.AppendLine("")
    }
    elseif ($fileMatches.Count -gt 1) {
        Write-Host "  AMBIGUOUS : $name ($($fileMatches.Count) matches)" -ForegroundColor Magenta
        [void]$fileBlock.AppendLine("")
        [void]$fileBlock.AppendLine("============================================================")
        [void]$fileBlock.AppendLine("FILE: $name")
        [void]$fileBlock.AppendLine("STATUS: AMBIGUOUS (multiple matches found)")
        [void]$fileBlock.AppendLine("MATCHES:")
        foreach ($m in $fileMatches) {
            $rel = Get-RepoRelativePath -FullPath $m.FullName -RootPathResolved $root
            [void]$fileBlock.AppendLine(" - $rel")
        }
        [void]$fileBlock.AppendLine("============================================================")
        [void]$fileBlock.AppendLine("")
    }
    else {
        $file     = $fileMatches[0]
        $absolute = $file.FullName
        $relative = Get-RepoRelativePath -FullPath $absolute -RootPathResolved $root

        [void]$fileBlock.AppendLine("")
        [void]$fileBlock.AppendLine("============================================================")
        [void]$fileBlock.AppendLine("FILE NAME: $name")
        [void]$fileBlock.AppendLine("RELATIVE : $relative")
        [void]$fileBlock.AppendLine("FULL PATH: $absolute")
        [void]$fileBlock.AppendLine("============================================================")
        [void]$fileBlock.AppendLine("")

        $content = Get-Content -LiteralPath $absolute -Raw
        [void]$fileBlock.AppendLine($content)
    }

    $blockText = $fileBlock.ToString()

    # If adding this block would breach the limit, flush the current chunk first
    if (($currentChunk.Length + $blockText.Length) -gt $ChunkLimit) {
        Emit-Chunk -Text $currentChunk.ToString() -IsFinal:$false -FilesIncluded $script:filesInChunk.ToArray()
        $currentChunk.Clear() | Out-Null
        $script:filesInChunk.Clear()
    }

    [void]$currentChunk.Append($blockText)
    $script:filesInChunk.Add($name)
}

# Emit final chunk — no ENTER prompt, just report and exit
Emit-Chunk -Text $currentChunk.ToString() -IsFinal:$true -FilesIncluded $script:filesInChunk.ToArray()