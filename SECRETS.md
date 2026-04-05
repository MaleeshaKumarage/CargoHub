# Secrets and Environment Configuration

Secrets live in `.env` (never committed). **When deploying**, sync them to GitHub Secrets so workflows and deployment platforms can use them.

---

## Local Development

### 1. Backend (API)

```powershell
# From repo root
Copy-Item .env.example .env
# Edit .env with your values
```

The API loads `.env` at startup via [DotNetEnv](https://github.com/tonerdo/dotnet-env).

### 2. Portal (Next.js)

```powershell
cd portal
Copy-Item .env.example .env.local
# Edit .env.local - NEXT_PUBLIC_API_URL=http://localhost:5299
```

### 3. EF Core Migrations

Run from repo root so `ConnectionStrings__DefaultConnection` is loaded:

```powershell
dotnet ef migrations add <Name> -p CargoHub.Infrastructure -s CargoHub.Api
```

---

## Sync .env → GitHub Secrets (Before Deploy)

**When deploying**, copy your `.env` values into GitHub repository secrets:

```powershell
# 1. Install GitHub CLI: https://cli.github.com/
# 2. Log in: gh auth login
# 3. From repo root, sync .env to GitHub Secrets:
./scripts/sync-secrets-to-github.ps1

# Dry run (preview without writing):
./scripts/sync-secrets-to-github.ps1 -DryRun

# Use a different env file (e.g. production):
./scripts/sync-secrets-to-github.ps1 -EnvFile .env.production
```

This runs `gh secret set --env-file .env`, which creates/updates one GitHub secret per line in `.env` (skipping comments and empty lines).

---

## All Possible Secrets (.env.example)

| Variable | Used For |
|----------|----------|
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string |
| `Bootstrap__Secret` | First SuperAdmin creation (one-time) |
| `Jwt__SigningKey` | JWT token signing (32+ chars) |
| `Jwt__Issuer` | JWT issuer claim |
| `Jwt__Audience` | JWT audience claim |
| `NEXT_PUBLIC_API_URL` | Portal → API URL (Vercel build) |
| `Cors__PortalOrigin` | CORS allowed origin |
| `Cors__PortalOrigins__0`, `__1`, … | Multiple CORS origins |
| `Smtp__Host` | SMTP server |
| `Smtp__Port` | SMTP port |
| `Smtp__UserName` | SMTP username |
| `Smtp__Password` | SMTP password |
| `Smtp__FromAddress` | From email address |
| `Courier__DHLExpress__BaseUrl` | DHL API base URL |
| `Courier__DHLExpress__BasicAuthBase64` | DHL Basic auth (base64) |
| `Courier__Matkahuolto__BookingUrl` | Matkahuolto booking endpoint |
| `Courier__HameenTavarataxi__CarrierEmail` | Hämeen Tavarataxi recipient |
| `Courier__HameenTavarataxi__TestEmail` | Test override email |
| `Branding__*` | App name, logo, colors (optional) |

### GitHub Actions only (repository secrets, not in `.env`)

| Secret | Used For |
|--------|----------|
| `DOCKERHUB_USERNAME` | **Docker — build & push** workflow: registry namespace for `cargohub` image |
| `DOCKERHUB_TOKEN` | **Docker — build & push**: Docker Hub access token (or password) paired with `DOCKERHUB_USERNAME` |

If either is missing, the workflow still **builds** the image to verify `Dockerfile.all-in-one` but does **not** push. Mac deploy expects a pushed image — set both secrets for full pipeline.

---

## Deployment (Render / Vercel)

After syncing to GitHub Secrets:

- **Render (API):** Add env vars in the Render dashboard (or use GitHub integration to pull from secrets). Keys match `.env` exactly.
- **Vercel (Portal):** Add `NEXT_PUBLIC_API_URL` in Vercel project settings.

See [DEPLOYMENT.md](DEPLOYMENT.md) for full setup.

---

## Security Checklist

- [ ] `.env` and `.env.local` are in `.gitignore` (never committed)
- [ ] `.env.example` contains only placeholders, no real secrets
- [ ] Run `sync-secrets-to-github.ps1` before deploying so GitHub Secrets are up to date
- [ ] Production uses strong random values for `Jwt__SigningKey` and `Bootstrap__Secret`
