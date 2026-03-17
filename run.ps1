# Start PostgreSQL in Docker, then run the API (migrations apply on startup).
# Requires: Docker Desktop running.
# Usage: .\run.ps1

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot

Write-Host "Starting PostgreSQL (Docker)..." -ForegroundColor Cyan
Push-Location $root
try {
    docker compose up -d --wait
    if ($LASTEXITCODE -ne 0) { throw "docker compose up failed" }
    Write-Host "Database ready." -ForegroundColor Green

    Write-Host "Running API (migrations will apply automatically)..." -ForegroundColor Cyan
    dotnet run --project CargoHub.Api --urls "http://localhost:5000"
} finally {
    Pop-Location
}
