# Run CargoHub with Docker

Any machine with Docker can run the full stack with one command.

---

## Option A: One package (simplest)

Single image: db + API + portal. One container.

```bash
docker compose -f docker-compose.one.yml up -d
```

Or with plain Docker:

```bash
docker run -d -p 3000:3000 -p 8080:8080 -v cargohub_data:/var/lib/postgresql/data --name cargohub maleesha404/cargohub:latest
```

- **Portal:** http://localhost:3000  
- **API:** http://localhost:8080  

---

## Option B: Separate images (3 containers)

```bash
docker compose -f docker-compose.pull.yml up -d
```

- **Portal:** http://localhost:3000  
- **API:** http://localhost:8080  

---

## Option C: Build from source

```bash
docker compose up -d
```

- **Portal:** http://localhost:3000  
- **API:** http://localhost:8080  

## First-time setup: Create admin user

After the stack is running, create the first SuperAdmin:

```powershell
# PowerShell
$body = @{ email = "admin@example.com"; password = "Admin123!"; fullName = "Admin" } | ConvertTo-Json
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
  -d '{"email":"admin@example.com","password":"Admin123!","fullName":"Admin"}'
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
