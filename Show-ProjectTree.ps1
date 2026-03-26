param (
    [string]$RootPath = "."
)

$excludeDirs = @(
    "bin",
    "obj",
    ".git",
    ".vs",
    ".idea",
    "TestResults",
    "node_modules"
)

$excludeFilePatterns = @(
    "*.log",
    "*.ps1"
)

function Show-Tree {
    param (
        [string]$Path,
        [string]$Indent = ""
    )

    $items = Get-ChildItem -Path $Path -Force | Where-Object {
        if ($_.PSIsContainer) {
            $excludeDirs -notcontains $_.Name
        }
        else {
            foreach ($pat in $excludeFilePatterns) {
                if ($_.Name -like $pat) { return $false }
            }
            return $true
        }
    } | Sort-Object { -not $_.PSIsContainer }, Name

    foreach ($item in $items) {
        Write-Output "$Indent$($item.Name)"

        if ($item.PSIsContainer) {
            Show-Tree -Path $item.FullName -Indent ("$Indent  ")
        }
    }
}

$fullPath = (Resolve-Path -LiteralPath $RootPath).Path

$treeText = @(
    "-----------------------------------------------------------------"
    "Project structure for: $fullPath"
    "-----------------------------------------------------------------"
    (Show-Tree -Path $fullPath)
) -join [Environment]::NewLine

$treeText | Set-Clipboard
Write-Host "Copied project structure to clipboard."
