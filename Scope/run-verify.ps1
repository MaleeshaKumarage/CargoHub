# Run after starting the API: dotnet run --project HiavaNet.Api --urls "http://localhost:5000"
# Requires PostgreSQL configured and running for the API to start.

$base = "http://localhost:5000"

Write-Host "=== Section 12: Health and Swagger ===" -ForegroundColor Cyan
try {
    $r = Invoke-RestMethod -Uri "$base/api/v1/health_" -Method Get
    Write-Host "GET /api/v1/health_ -> OK" -ForegroundColor Green
    Write-Host ($r | ConvertTo-Json)
} catch { Write-Host "FAIL: $_" -ForegroundColor Red }

try {
    Invoke-RestMethod -Uri "$base/swagger/v1/swagger.json" -Method Get | Out-Null
    Write-Host "GET /swagger/v1/swagger.json -> OK" -ForegroundColor Green
} catch { Write-Host "FAIL: $_" -ForegroundColor Red }

Write-Host "`n=== Section 02: Portal register / login ===" -ForegroundColor Cyan
# Add more sections as we implement them.
