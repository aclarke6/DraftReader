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
    [string]$OutputFile = ""
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

  -h | -ShowHelp
      Show this help text and exit.

NOTES
  If you use positional filenames, put -RootPath and -OutputFile BEFORE the filenames:
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

$chunkLimit = 10000
$currentChunk = New-Object System.Text.StringBuilder
$script:chunkIndex = 1

function Emit-Chunk {
    param(
        [string]$Text,
        [bool]$IsFinal = $false
    )

    if ([string]::IsNullOrWhiteSpace($Text)) { return }

    # Build wrapped chunk text
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
    Write-Host "=== Emitting chunk #$script:chunkIndex ==="
    Write-Host "Characters: $($finalText.Length)"
    Write-Host "Copied to clipboard. Press ENTER for next chunk..."
    Write-Host ""

    $finalText | Set-Clipboard

    if ($OutputFile -ne "") {
        $outPath = [IO.Path]::ChangeExtension($OutputFile, ".$script:chunkIndex.txt")
        $finalText | Out-File -FilePath $outPath -Encoding utf8
        Write-Host "Also wrote: $outPath"
    }

    $null = Read-Host
    $script:chunkIndex++

}

foreach ($name in $FileNames) {

    # Build the block for this file
    $fileBlock = New-Object System.Text.StringBuilder

    $matches = @(Find-FileByName -FileName $name)

    if (-not $matches -or @($matches).Count -eq 0) {
        [void]$fileBlock.AppendLine("")
        [void]$fileBlock.AppendLine("============================================================")
        [void]$fileBlock.AppendLine("FILE: $name")
        [void]$fileBlock.AppendLine("STATUS: MISSING (not found under $root)")
        [void]$fileBlock.AppendLine("============================================================")
        [void]$fileBlock.AppendLine("")
    }
    elseif ($matches.Count -gt 1) {
        [void]$fileBlock.AppendLine("")
        [void]$fileBlock.AppendLine("============================================================")
        [void]$fileBlock.AppendLine("FILE: $name")
        [void]$fileBlock.AppendLine("STATUS: AMBIGUOUS (multiple matches found)")
        [void]$fileBlock.AppendLine("MATCHES:")
        foreach ($m in $matches) {
            $rel = Get-RepoRelativePath -FullPath $m.FullName -RootPathResolved $root
            [void]$fileBlock.AppendLine(" - $rel")
        }
        [void]$fileBlock.AppendLine("============================================================")
        [void]$fileBlock.AppendLine("")
    }
    else {
        $file = $matches[0]
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

    # Check if adding this block would exceed the chunk limit
    if (($currentChunk.Length + $blockText.Length) -gt $chunkLimit) {
        Emit-Chunk -Text $currentChunk.ToString()
        $currentChunk.Clear() | Out-Null
    }

    # Add the block to the current chunk
    [void]$currentChunk.Append($blockText)
}

# Emit final chunk
Emit-Chunk -Text $currentChunk.ToString() -IsFinal:$true


Write-Host "All chunks emitted."
