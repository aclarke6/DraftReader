$env:PGPASSWORD = (dotnet user-secrets list --project DraftReader.Web | Where-Object { $_ -match "PostgresPassword" } | ForEach-Object { $_ -replace "PostgresPassword = ", "" })
$pgArgs = @("-U", "postgres", "-d", "draftreader") + $args
& "C:\Program Files\PostgreSQL\18\bin\psql.exe" @pgArgs
