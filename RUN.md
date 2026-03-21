# Run CargoHub with Docker

One image. One pull. One command.

---

## Pull and run (recommended)

Single image with db + API + portal:

```bash
docker pull maleesha404/cargohub:latest
docker run -d -p 3000:3000 -p 8080:8080 -v cargohub_data:/var/lib/postgresql/data --name cargohub maleesha404/cargohub:latest
```

Or with compose:

```bash
docker compose -f docker-compose.one.yml up -d
```

- **Portal:** http://localhost:3000  
- **API:** http://localhost:8080  

---

## Build from source

```bash
docker compose up -d
```

- **Portal:** http://localhost:3000  
- **API:** http://localhost:8080  

## First-time setup: Create admin user

After the stack is running, create the first SuperAdmin:

```powershell
# PowerShell
$body = @{ email = "admin@example.com"; password = "Admin123!"; displayName = "Admin" } | ConvertTo-Json
Invoke-RestMethod -Uri "http://localhost:8080/api/v1/portal/bootstrap-superadmin" `
  -Method Post -ContentType "application/json" `
  -Headers @{ "X-Bootstrap-Secret" = "SuperAdminBootstrapSecret" } `
  -Body $body
```

```bash
# Bash / curl
curl -X POST http://localhost:8080/api/v1/portal/bootstrap-superadmin \
  -H "Content-Type: application/json" \
  -H "X-Bootstrap-Secret: SuperAdminBootstrapSecret" \
  -d '{"email":"admin@example.com","password":"Admin123!","displayName":"Admin"}'
```

Then log in at http://localhost:3000 with `admin@example.com` / `Admin123!`.

## Optional: Customize with .env

Copy `.env.example` to `.env` and change secrets. For Docker, keep `NEXT_PUBLIC_API_URL=http://localhost:8080` (API port in compose).

## Stop

```bash
docker compose down
```

## Data persistence

PostgreSQL data is stored in a Docker volume. Use `docker compose down -v` to remove it.

---

## Self-hosted runner (Mac Mini / home server)

After you register a **self-hosted GitHub Actions runner** on the machine where Docker runs:

1. **Auto-deploy:** Pushes to `main` / `master` run **Docker Hub Push**, then **Deploy (self-hosted)** pulls `maleesha404/cargohub:latest` and runs `docker compose -f docker-compose.one.yml up -d`.

2. **Manual deploy:** GitHub → **Actions** → **Deploy (self-hosted)** → **Run workflow**.

3. **Secrets (optional):** Repo → **Settings** → **Secrets and variables** → **Actions** — add `BOOTSTRAP__SECRET`, `JWT__SIGNING_KEY`, `CORS__PORTAL_ORIGIN` if you don’t use a local env file.

4. **Local env file (optional):** On the runner, create `~/.cargohub.env` (e.g. `CORS__PORTAL_ORIGIN=https://your-portal.ngrok-free.app`) so compose picks it up during deploy.

5. **Public URL (ngrok):** Expose ports `3000` and `8080` (or use a reverse proxy). Set `CORS__PORTAL_ORIGIN` to your **portal** HTTPS URL. The baked-in `NEXT_PUBLIC_API_URL` in the public image points at `http://localhost:8080`; for a single demo URL you may need a custom build or two ngrok tunnels — see `DEPLOYMENT.md` if present.

6. **Runner + Docker:** The user running the runner must be able to run `docker` (e.g. in the `docker` group).
