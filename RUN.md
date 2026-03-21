# Run CargoHub with Docker

One image. One pull. One command.

---

## Pull and run (recommended)

Single image with db + API + portal:

```bash
docker pull maleesha404/cargohub:latest
docker run -d -p 3000:3000 -p 8080:8080 -p 4040:4040 -v cargohub_data:/var/lib/postgresql/data --name cargohub maleesha404/cargohub:latest
```

**Public URLs (ngrok inside the image):** set `-e NGROK_AUTHTOKEN=your_token` on `docker run` (or use `docker-compose.one.yml`). The image includes the ngrok agent; tunnels start automatically. Then open **http://localhost:4040** on the host for the ngrok dashboard and JSON with **`public_url`** for portal (3000) and API (8080). You do **not** need ngrok installed on the host.

Or with compose:

```bash
docker compose -f docker-compose.one.yml up -d
```

- **Portal:** http://localhost:3000  
- **API:** http://localhost:8080  
- **ngrok dashboard (if token set):** http://localhost:4040  

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
| 3. ngrok | **Inside the container** | If `NGROK_AUTHTOKEN` is set (GitHub secret), ngrok starts in the image; use **http://localhost:4040** on the Mac for public URLs (no ngrok binary on the host required) |

**Secrets:** `DOCKERHUB_USERNAME`, `DOCKERHUB_TOKEN` (required). Optional: `NGROK_AUTHTOKEN`, `CORS__PORTAL_ORIGIN`, `BOOTSTRAP__SECRET`, `JWT__SIGNING_KEY`.

**Manual run:** Actions → **Docker Hub + Mac deploy + ngrok** → **Run workflow**. The step **Show public URLs (ngrok)** prints **Portal** and **API** links in the job log, adds **GitHub Actions notices** (hover the run), and writes a **markdown table** to the job **Summary** tab.

### Other workflows

| Workflow | When it runs | What it tests |
|----------|----------------|---------------|
| **PR Validation** | PRs to `main` / `master` / `development`, or **Run workflow** | `npm test` + coverage, `dotnet build` / `dotnet test` + coverage (75% gates) |
| **Docker validate (all-in-one)** | Same PRs, or **Run workflow** | `Dockerfile.all-in-one` **builds** (no push to registry) |

**Suggested order to test:** PR Validation → Docker validate → then push to `main` (full release pipeline).

---

## Self-hosted runner (Mac Mini / home server)

1. Install **Docker** and register a **self-hosted GitHub Actions runner** on the Mac (**ngrok on the host is optional** — the app image can run ngrok inside the container).

2. **Push to `main`, `master`, or `development`** (or **Run workflow**): **Docker Hub + Mac deploy + ngrok** builds the image (with embedded ngrok agent), deploys, and passes **`NGROK_AUTHTOKEN`** into the container via compose.

3. **Secrets:** `DOCKERHUB_USERNAME` + `DOCKERHUB_TOKEN` (required). **`NGROK_AUTHTOKEN`** — [ngrok dashboard](https://dashboard.ngrok.com/get-started/your-authtoken); add as repo secret. Optional: `BOOTSTRAP__SECRET`, `JWT__SIGNING_KEY`, `CORS__PORTAL_ORIGIN`.

4. **Public URLs:** On the Mac, open **http://localhost:4040** (mapped from the container) for the ngrok UI and **`public_url`** list. Or read the **Show ngrok public URLs** step in the Actions run.

5. **Local env (optional):** `~/.cargohub.env` on the Mac for extra compose vars.

6. **`CORS__PORTAL_ORIGIN`:** Set to your **portal** HTTPS ngrok URL when testing from the internet. The baked-in `NEXT_PUBLIC_API_URL` may still point at `localhost:8080` for API calls from the browser in some setups — use the **API** tunnel URL or a custom build if needed.

7. **Runner user** must be in the **`docker`** group (`sudo usermod -aG docker $USER` and re-login).

### `Bind for 0.0.0.0:8080 failed: port is already allocated`

Something else is using **3000** or **8080** (often an old `cargohub` container or a manual `docker run`). The deploy workflow now runs **`compose down`**, removes **`cargohub`**, then removes any container bound to those ports before starting.

**Manual fix on the Mac:**

```bash
docker compose -f docker-compose.one.yml down --remove-orphans
docker rm -f cargohub 2>/dev/null || true
docker ps -q --filter publish=8080 | xargs -r docker rm -f
docker ps -q --filter publish=3000 | xargs -r docker rm -f
```

Then run the workflow again or `docker compose -f docker-compose.one.yml up -d`.

### Deploy ran but `docker ps` looks unchanged

- **Same image tag:** `latest` was updated on the registry, but Compose reused the old container. The repo uses `pull_policy: always` and `docker compose up -d --force-recreate` so the next deploy recreates the container. To fix manually:  
  `docker compose -f docker-compose.one.yml pull && docker compose -f docker-compose.one.yml up -d --force-recreate`

### No deploy job on the Mac (second job missing)

- **Workflow file must exist on `main`.** Merge CI YAML to `main` so pushes trigger **Docker Hub + Mac deploy + ngrok**.
- **First job failed (Docker Hub):** The Mac job only runs if **Build and push image** succeeds — check secrets and build logs.
- **Runner offline:** **Settings** → **Actions** → **Runners** — idle. Terminal: **Listening for jobs**.
- **Wrong repo:** Self-hosted runners are **per repository**.
