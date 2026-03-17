# Sync .env to GitHub repository secrets.
# Run from repo root. Requires: gh CLI (gh auth login), .env file.
#
# Usage: ./scripts/sync-secrets-to-github.ps1
#        ./scripts/sync-secrets-to-github.ps1 -EnvFile .env.production
#        ./scripts/sync-secrets-to-github.ps1 -DryRun

param(
    [string]$EnvFile = ".env",
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$envPath = Join-Path $repoRoot $EnvFile

if (-not (Test-Path $envPath)) {
    Write-Error "Env file not found: $envPath. Copy .env.example to .env and fill in values."
    exit 1
}

# Filter out comments and empty lines for dry-run display
$lines = Get-Content $envPath | Where-Object {
    $_ -match '^\s*[A-Za-z_]' -and $_ -notmatch '^\s*#'
}
$count = ($lines | Where-Object { $_ -match '=' }).Count

if ($DryRun) {
    Write-Host "Dry run: would sync $count secrets from $envPath to GitHub" -ForegroundColor Cyan
    $lines | ForEach-Object {
        if ($_ -match '^([^=]+)=') {
            $key = $matches[1].Trim()
            Write-Host "  - $key"
        }
    }
    Write-Host "`nRun without -DryRun to sync."
    exit 0
}

# Ensure gh CLI is installed (skip for dry run)
$ghCmd = Get-Command gh -ErrorAction SilentlyContinue
$ghExe = if ($ghCmd) { $ghCmd.Source } else { $null }
if (-not $ghExe) {
    $ghExe = "C:\Program Files\GitHub CLI\gh.exe"
    if (-not (Test-Path $ghExe)) {
        $ghExe = "$env:LOCALAPPDATA\Programs\GitHub CLI\gh.exe"
    }
    if (-not (Test-Path $ghExe)) {
        Write-Error "GitHub CLI (gh) not found. Install: https://cli.github.com/ Restart terminal after install."
        exit 1
    }
}

Push-Location $repoRoot
try {
    Write-Host "Syncing $count secrets from $envPath to GitHub repository..." -ForegroundColor Cyan
    $result = & $ghExe secret set --env-file $envPath 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Done. Secrets are now available in GitHub Actions." -ForegroundColor Green
    } else {
        Write-Error $result
        exit 1
    }
} finally {
    Pop-Location
}
