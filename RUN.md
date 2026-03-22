# Run CargoHub with Docker

One image. One pull. One command.

---

## Pull and run (recommended)

Single image with db + API + portal:

```bash
docker pull maleesha404/cargohub:latest
docker run -d -p 8888:8888 -p 3000:3000 -p 8080:8080 -p 4040:4040 -v cargohub_data:/var/lib/postgresql/data --name cargohub maleesha404/cargohub:latest
```

- **`:8888`** — **nginx**: same origin for UI + API (what you should expose behind **one** ngrok tunnel). Example: `https://<subdomain>.ngrok-free.app/en/` and API at `.../api/v1/...`.
- **`:3000` / `:8080`** — direct Next.js / API (debug or local-only).

**Public URLs (ngrok inside the image):** set `-e NGROK_AUTHTOKEN=your_token` on `docker run` (or use `docker-compose.one.yml`). The image includes the ngrok agent; tunnels start automatically. **`ngrok.yml` tunnels port `8888` (nginx)** so one HTTPS URL serves the portal and `/api`. **`ngrok.yml` uses `web_addr: 0.0.0.0:4040`** so the host can reach the ngrok API on **http://localhost:4040**. You do **not** need ngrok installed on the host. If URLs are empty in CI, **pull the latest image** (older images may not include in-container ngrok).

**Paths:** The portal uses **locale-prefixed routes** (e.g. **`/en/login`**, **`/en/dashboard`**). **`/dashboard` alone returns 404** — use **`/en/dashboard`** (or your default locale).

Or with compose:

```bash
docker compose -f docker-compose.one.yml up -d
```

- **Public (nginx, same as ngrok):** http://localhost:8888  
- **Portal (direct):** http://localhost:3000  
- **API (direct):** http://localhost:8080  
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

### Pipeline layout (single responsibility)

| Workflow | Jobs | Purpose |
|----------|------|--------|
| **Docker — build & push image** | Build & push on Ubuntu; optional issue on build failure | Runs on **every** push; **duplicate builds on the same branch cancel** so the registry always gets the latest commit quickly. |
| **Docker — deploy on Mac** | Deploy on self-hosted runner after a **successful** build | Triggered by `workflow_run` (not a second push pipeline). **One global queue** for deploy (`docker-mac-deploy-self-hosted`) so **main / master / development** don’t each try to grab the Mac at once — avoids queue deadlocks. |
| **PR Validation** | `1 — Portal` ∥ `2 — Backend` → `3 — Coverage thresholds` → `4 — Open issue on failure` | Portal and backend CI run **in parallel**; coverage gates run only if both succeed. |

### Automatic GitHub issues on failure

When **PR Validation** or **Docker build / Mac deploy** fails, a workflow may open a **new issue** with a link to the failed run. Fork PRs do not get an issue from PR Validation (token limits).

### One automated release (push to `main` / `master` / `development`)

**Workflows: Docker — build & push image** + **Docker — deploy on Mac**

| Step | Where it runs | What happens |
|------|----------------|----------------|
| 1. Build & push | GitHub (Ubuntu) | Builds `Dockerfile.all-in-one`, pushes `cargohub:latest` + `:sha` to Docker Hub |
| 2. Deploy on Mac | Your self-hosted runner | `docker compose pull` + `up -d --force-recreate`, smoke tests |
| 3. ngrok | **Inside the container** | If `NGROK_AUTHTOKEN` is set (GitHub secret), ngrok starts in the image; use **http://localhost:4040** on the Mac for public URLs (no ngrok binary on the host required) |

**Secrets:** `DOCKERHUB_USERNAME`, `DOCKERHUB_TOKEN` (required). Optional: `NGROK_AUTHTOKEN`, `CORS__PORTAL_ORIGIN`, `BOOTSTRAP__SECRET`, `JWT__SIGNING_KEY`.

**Manual run:** Actions → **Docker — build & push image** → **Run workflow** (build only on Ubuntu). **Deploy on Mac** runs automatically after that workflow **succeeds** (see **Docker — deploy on Mac** in the run list). Ngrok URLs appear in the deploy job log and **Summary**.

### Other workflows

| Workflow | When it runs | What it tests |
|----------|----------------|---------------|
| **PR Validation** | PRs to `main` / `master` / `development`, or **Run workflow** | Portal + backend jobs in **parallel**; then **coverage-gates** (75% min) |
| **Docker validate (all-in-one)** | Same PRs, or **Run workflow** | `Dockerfile.all-in-one` **builds** (no push to registry) |

**Suggested order to test:** PR Validation → Docker validate → then push to `main` (full release pipeline).

**Branch protection:** If you require status checks before merge, set required checks to **`portal-ci`**, **`backend-ci`**, and **`coverage-gates`** (replacing any old **`build-and-test`** check).

---

## Self-hosted runner (Mac Mini / home server)

1. Install **Docker** and register a **self-hosted GitHub Actions runner** on the Mac (**ngrok on the host is optional** — the app image can run ngrok inside the container).

2. **Push to `main`, `master`, or `development`**: **Docker — build & push image** runs on GitHub-hosted runners; then **Docker — deploy on Mac** runs on your Mac (pulls the image, starts the stack, optional ngrok via **`NGROK_AUTHTOKEN`**).

3. **Secrets:** `DOCKERHUB_USERNAME` + `DOCKERHUB_TOKEN` (required). **`NGROK_AUTHTOKEN`** — [ngrok dashboard](https://dashboard.ngrok.com/get-started/your-authtoken); add as repo secret. Optional: `BOOTSTRAP__SECRET`, `JWT__SIGNING_KEY`, `CORS__PORTAL_ORIGIN`.

4. **Public URLs:** On the Mac, open **http://localhost:4040** (mapped from the container) for the ngrok UI and **`public_url`** list. Or read the **Show ngrok public URLs** step in the Actions run.

5. **Local env (optional):** `~/.cargohub.env` on the Mac for extra compose vars.

6. **`CORS__PORTAL_ORIGIN`:** Set to your **HTTPS ngrok origin** (same URL as the **public** tunnel, e.g. `https://xyz.ngrok-free.app`) so the API allows browser requests from the portal. Without this, login/API calls from the internet can fail with CORS.

7. **Runner user** must be in the **`docker`** group (`sudo usermod -aG docker $USER` and re-login).

8. **`workflow_run` requirement:** The file **`.github/workflows/docker-deploy-mac.yml` must exist on the repository default branch** (usually `main`) or GitHub will **not** trigger **Docker — deploy on Mac** after builds. Merge CI changes to `main` (or change the default branch in repo settings).

### Deploy stuck on **“Set up job”** or long **Queued**

- **One global deploy queue** — pushes to `main` / `master` / `development` each finish **build** on Ubuntu first; **deploy** runs **one at a time** on the Mac. Extra deploys **wait in line** (FIFO), which can look slow but avoids parallel half-states on a single runner.
- If **Set up job** never finishes: restart the runner service; check disk space and **`_diag`** logs.

### `Bind for 0.0.0.0:8080 failed: port is already allocated`

Something else is using **8888**, **3000**, or **8080** (often an old `cargohub` container or a manual `docker run`). The deploy workflow now runs **`compose down`**, removes **`cargohub`**, then removes any container bound to those ports before starting.

**Manual fix on the Mac:**

```bash
docker compose -f docker-compose.one.yml down --remove-orphans
docker rm -f cargohub 2>/dev/null || true
docker ps -q --filter publish=8888 | xargs -r docker rm -f
docker ps -q --filter publish=8080 | xargs -r docker rm -f
docker ps -q --filter publish=3000 | xargs -r docker rm -f
```

Then run the workflow again or `docker compose -f docker-compose.one.yml up -d`.

### `docker compose pull` / `auth.docker.io` timeout on the Mac

Anonymous pulls from Docker Hub are **rate-limited** and often **slow**; `Client.Timeout exceeded while awaiting headers` usually means the registry or auth endpoint didn’t respond in time.

- **Docker — deploy on Mac** logs in with **`DOCKERHUB_USERNAME`** / **`DOCKERHUB_TOKEN`** before `compose pull`, retries pulls, and sets a longer **`COMPOSE_HTTP_TIMEOUT`**.
- Ensure those **same repo secrets** exist and are available to the self-hosted runner job.
- **Manual pull on the Mac:** `docker login` then `docker compose -f docker-compose.one.yml pull`.

### DNS / network errors pulling from `registry-1.docker.io` (self-hosted runner)

If the deploy job fails on **Pull image** with messages like **`lookup registry-1.docker.io ... i/o timeout`**, **`context deadline exceeded`**, or **`network is unreachable`** (often on IPv6), the runner’s **network or DNS** is flaky — not the app repo.

- **Re-run** the failed **Docker — deploy on Mac** workflow once connectivity is stable (`gh run rerun <run-id>` or Actions → **Re-run jobs**).
- On the runner host: fix **DNS** (e.g. use reliable resolvers in **`/etc/resolv.conf`** or Docker **`daemon.json`** `dns`), ensure **outbound HTTPS** to Docker Hub, and if IPv6 is broken either fix routing or **prefer IPv4** for Docker/registry (runner/OS-specific).

### Deploy ran but `docker ps` looks unchanged

- **Same image tag:** `latest` was updated on the registry, but Compose reused the old container. The repo uses `pull_policy: always` and `docker compose up -d --force-recreate` so the next deploy recreates the container. To fix manually:  
  `docker compose -f docker-compose.one.yml pull && docker compose -f docker-compose.one.yml up -d --force-recreate`

### No deploy job on the Mac (second job missing)

- **Workflow files must exist on the default branch** (`workflow_run` reads them from there). Merge CI YAML to `main` so **Docker — deploy on Mac** can trigger after builds.
- **First job failed (Docker Hub):** The Mac job only runs if **Build and push image** succeeds — check secrets and build logs.
- **Runner offline:** **Settings** → **Actions** → **Runners** — idle. Terminal: **Listening for jobs**.
- **Wrong repo:** Self-hosted runners are **per repository**.
