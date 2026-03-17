# Apply IsDraft column to bookings.Bookings so Create booking works.
# Uses same DB as appsettings.Development.json: localhost:5433, database portal.
# Requires: psql in PATH (from PostgreSQL client), or run apply-IsDraft-migration.sql in pgAdmin/DBeaver.

$ErrorActionPreference = "Stop"
$sqlFile = Join-Path $PSScriptRoot "apply-IsDraft-migration.sql"
if (-not (Test-Path $sqlFile)) { Write-Error "Not found: $sqlFile"; exit 1 }

$env:PGPASSWORD = "postgres"
try {
    & psql -h localhost -p 5433 -U postgres -d portal -f $sqlFile
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    Write-Host "IsDraft migration applied. Try Create booking again."
} catch {
    Write-Host "psql not found or failed. Run the SQL manually:"
    Write-Host "  File: $sqlFile"
    Write-Host "  In pgAdmin/DBeaver, connect to localhost:5433 / portal and execute the file."
    exit 1
}
