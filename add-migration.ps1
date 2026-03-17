# Create the initial EF Core migration (Identity + Bookings + Companies).
# IMPORTANT: Stop the API first (Stop Debugging in Visual Studio / close the running app).
# Otherwise the build will fail with "file is being used by another process".
# Requires: dotnet-ef tool (install with: dotnet tool install --global dotnet-ef)
# After this runs, start the API again; Migrate() on startup will apply the migration.

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot

Push-Location $root
try {
    dotnet ef migrations add InitialCreate -p HiavaNet.Infrastructure -s HiavaNet.Api
    if ($LASTEXITCODE -ne 0) { throw "Migration command failed" }
    Write-Host "Done. Start the API; migrations will apply on startup." -ForegroundColor Green
} finally {
    Pop-Location
}
