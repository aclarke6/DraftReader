Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
Set-Location "C:\Users\alast\source\repos\DraftReader"

$sqlFile = "temp-preview-readers.sql"

@'
SELECT "Id", "Email", "DisplayName", "IsActive", "CreatedAt"
FROM public."AppUsers"
WHERE "Role" = 'BetaReader'
  AND (
    "DisplayName" = 'Pending'
    OR "IsActive" = FALSE
  )
ORDER BY "CreatedAt";
'@ | Set-Content -Path $sqlFile -Encoding UTF8