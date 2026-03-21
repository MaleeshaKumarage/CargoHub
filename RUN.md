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

## Test the GitHub Actions pipeline

### One automated release (push to `main` / `master` / `development`)

**Workflow: Docker Hub + Mac deploy + ngrok**

| Step | Where it runs | What happens |
|------|----------------|----------------|
| 1. Build & push | GitHub (Ubuntu) | Builds `Dockerfile.all-in-one`, pushes `cargohub:latest` + `:sha` to Docker Hub |
| 2. Deploy on Mac | Your self-hosted runner | `docker compose pull` + `up -d --force-recreate`, smoke tests |
| 3. ngrok | Same Mac | Restarts tunnels from repo `ngrok.yml` (portal **3000**, API **8080**) if `NGROK_AUTHTOKEN` is set |

**Secrets:** `DOCKERHUB_USERNAME`, `DOCKERHUB_TOKEN` (required). Optional: `NGROK_AUTHTOKEN`, `CORS__PORTAL_ORIGIN`, `BOOTSTRAP__SECRET`, `JWT__SIGNING_KEY`.

**Manual run:** Actions → **Docker Hub + Mac deploy + ngrok** → **Run workflow**.

### Other workflows

| Workflow | When it runs | What it tests |
|----------|----------------|---------------|
| **PR Validation** | PRs to `main` / `master` / `development`, or **Run workflow** | `npm test` + coverage, `dotnet build` / `dotnet test` + coverage (75% gates) |
| **Docker validate (all-in-one)** | Same PRs, or **Run workflow** | `Dockerfile.all-in-one` **builds** (no push to registry) |

**Suggested order to test:** PR Validation → Docker validate → then push to `main` (full release pipeline).

---

## Self-hosted runner (Mac Mini / home server)

1. Install **Docker**, **ngrok** (on `PATH`), and register a **self-hosted GitHub Actions runner** on the Mac.

2. **Push to `main`, `master`, or `development`** (or **Run workflow**): **Docker Hub + Mac deploy + ngrok** runs end-to-end — no separate deploy workflow.

3. **Secrets:** `DOCKERHUB_USERNAME` + `DOCKERHUB_TOKEN` (required). **`NGROK_AUTHTOKEN`** — get from [ngrok dashboard](https://dashboard.ngrok.com/get-started/your-authtoken); add as repo secret so each deploy restarts tunnels. Optional: `BOOTSTRAP__SECRET`, `JWT__SIGNING_KEY`, `CORS__PORTAL_ORIGIN`.

4. **Local env (optional):** `~/.cargohub.env` on the Mac for extra compose vars (merged during deploy).

5. **Remote browser + API:** The pipeline starts **two** tunnels (`ngrok.yml`: portal `3000`, API `8080`). Set **`CORS__PORTAL_ORIGIN`** to your **portal** ngrok HTTPS URL (GitHub secret or `~/.cargohub.env`). The public image may still call `http://localhost:8080` for the API from the browser — for full remote demos you may need a custom image with `NEXT_PUBLIC_API_URL` pointing at the **API** ngrok URL, or test from the same machine.

6. **Runner user** must be in the **`docker`** group (`sudo usermod -aG docker $USER` and re-login).

### Deploy ran but `docker ps` looks unchanged

- **Same image tag:** `latest` was updated on the registry, but Compose reused the old container. The repo uses `pull_policy: always` and `docker compose up -d --force-recreate` so the next deploy recreates the container. To fix manually:  
  `docker compose -f docker-compose.one.yml pull && docker compose -f docker-compose.one.yml up -d --force-recreate`

### No deploy job on the Mac (second job missing)

- **Workflow file must exist on `main`.** Merge CI YAML to `main` so pushes trigger **Docker Hub + Mac deploy + ngrok**.
- **First job failed (Docker Hub):** The Mac job only runs if **Build and push image** succeeds — check secrets and build logs.
- **Runner offline:** **Settings** → **Actions** → **Runners** — idle. Terminal: **Listening for jobs**.
- **Wrong repo:** Self-hosted runners are **per repository**.
