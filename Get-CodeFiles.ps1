(Get-ChildItem -Recurse -Filter *.cs -File |
    Where-Object {
        $_.FullName -notmatch '\\(bin|obj|Debug|Release|\.nuget)\\' -and
        $_.Name -notmatch '\.g(\.i)?\.cs$' -and
        $_.Name -notmatch 'AssemblyInfo\.cs$' -and
        $_.FullName -notmatch 'wpftmp'
    }
).Name -join " " | Set-Clipboard
Write-Host "Copied file list to clipboard."