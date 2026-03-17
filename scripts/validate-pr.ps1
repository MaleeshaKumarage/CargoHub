# Local PR validation — mirrors GitHub Actions
# Run before pushing: .\scripts\validate-pr.ps1

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)

Push-Location $root
try {
    Write-Host "=== Restore ===" -ForegroundColor Cyan
    dotnet restore CargoHub.Backend.sln
    if ($LASTEXITCODE -ne 0) { throw "Restore failed" }

    Write-Host "`n=== Build ===" -ForegroundColor Cyan
    dotnet build CargoHub.Backend.sln --no-restore --configuration Release
    if ($LASTEXITCODE -ne 0) { throw "Build failed" }

    Write-Host "`n=== Test (with coverage) ===" -ForegroundColor Cyan
    dotnet test CargoHub.Backend.sln --no-build --configuration Release --verbosity normal `
      --collect:"XPlat Code Coverage" --results-directory ./TestResults --settings coverlet.runsettings
    if ($LASTEXITCODE -ne 0) { throw "Tests failed" }

    Write-Host "`n=== All checks passed ===" -ForegroundColor Green
} finally {
    Pop-Location
}
