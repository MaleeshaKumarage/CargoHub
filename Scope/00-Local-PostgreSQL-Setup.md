# Local PostgreSQL setup (for testing)

The API uses PostgreSQL. You can run the database in **Docker** (recommended) or install it locally.

---

## Option 1: Docker (recommended)

**Requires:** [Docker Desktop](https://www.docker.com/products/docker-desktop/) installed and running.

### Run from Visual Studio (F5)

When you **start the solution from Visual Studio** (F5 or Start), the API project will:

1. Detect that it’s running in **Development** and look for `docker-compose.yml` in the solution directory.
2. Run **`docker compose up -d --wait`** so the PostgreSQL container is up and healthy.
3. Then start the API and apply EF Core migrations as usual.

So you only need to have **Docker Desktop running**; then press F5 and the DB and API start together.

**If you previously used the old database name (CargoHub):** The Docker setup now creates a database named `portal` and uses a new volume `portal_pgdata`. To start clean: run `docker compose down -v` (this removes the old volume), then start again (F5 or `.\run.ps1`). The new container will create the `portal` database. To keep existing data, you would need to rename the database inside PostgreSQL or attach the old volume manually.

**First-time setup (create database tables):** The first time you run the app, the database has no tables yet. You need to create the initial migration **once** (with the API **stopped**):

1. Stop the API (stop debugging in Visual Studio).
2. In a terminal at the solution root run:  
   `.\add-migration.ps1`  
   (If you don’t have the `dotnet-ef` tool: `dotnet tool install --global dotnet-ef` then run the script again.)
3. Start the API again (F5). On startup it will apply the migration and create all tables (e.g. `AspNetUsers`, `Bookings`, `Companies`).

**If you see "column a.IsActive does not exist":** The `AddUserIsActive` migration adds the `IsActive` column to users. If your DB was created before that migration existed, apply it once:

- **With Docker:** from the solution root run  
  `Get-Content Scope\apply-IsActive-migration.sql -Raw | docker exec -i portal-db psql -U postgres -d portal`
- **Or** stop the API and run  
  `dotnet ef database update --project CargoHub.Infrastructure --startup-project CargoHub.Api`

### Run from command line

From the **solution root** (`CargoHub.Backend`):

```powershell
.\run.ps1
```

This runs `docker compose up -d --wait` and then `dotnet run` for the API.

**Connection:** The container is exposed on **port 5433** (to avoid clashing with a local PostgreSQL on 5432). The app uses `Host=localhost;Port=5433;Database=portal;Username=postgres;Password=postgres` from `appsettings.Development.json`.

**Use pgAdmin with Docker:** In pgAdmin, add a server with host `localhost`, port **5433**, user `postgres`, password `postgres` to inspect the `portal` database.

**Database schemas:** Tables are grouped by schema: **auth** (Identity: AspNetUsers, AspNetRoles, etc.), **companies** (Companies, address books), **bookings** (Bookings), **couriers** (reserved for future). In pgAdmin, expand **portal** → **Schemas** → choose a schema → **Tables**.

**Useful commands:**

| Command | Description |
|--------|-------------|
| **F5 in Visual Studio** or `.\run.ps1` | Start DB + API |
| `docker compose up -d` | Start only the DB |
| `docker compose down` | Stop the DB container (data kept in a volume) |
| `docker compose down -v` | Stop the DB and delete its data volume |

### If you get "relation AspNetUsers does not exist"

The database has no tables yet. Create the initial migration **once** with the API **stopped**:

1. Stop the API (stop debugging in Visual Studio).
2. At the solution root run: `.\add-migration.ps1`  
   (Install the EF tool first if needed: `dotnet tool install --global dotnet-ef`)
3. Start the API again; it will apply the migration and create the tables.

### If you get "password authentication failed for user postgres"

- **You're hitting the wrong server:** The app is set to use **port 5433** for the Docker DB. If you still see this error, something may be overriding the connection string (e.g. another config or env var pointing at port 5432 and a different PostgreSQL).
- **Docker volume was created with a different password:** Remove the volume and start clean:
  ```powershell
  docker compose down -v
  ```
  Then start the solution again (F5 or `.\run.ps1`). The new volume will use password `postgres`.
- **Using local PostgreSQL instead of Docker:** If you want to use a local server on 5432, set `appsettings.Development.json` to that server and the password you use for the `postgres` user (see Option 2/3 below).

---

## Option 2: PostgreSQL already installed (e.g. with pgAdmin)

If you installed pgAdmin from the [pgAdmin installer](https://www.pgadmin.org/download/) or the [PostgreSQL for Windows](https://www.postgresql.org/download/windows/) installer, the installer often includes the PostgreSQL server.

1. **Check if the server is running**
   - Open **pgAdmin**.
   - In the left tree, under **Servers**, try **PostgreSQL** (or your server). If it connects, the server is running.
   - If there is no server or it won’t connect, start the service:
     - Windows: **Services** → find **postgresql-x64-&lt;version&gt;** → Start (or set Startup type: Automatic).
     - Or from an elevated Command Prompt: `pg_ctl -D "C:\Program Files\PostgreSQL\<version>\data" start` (adjust path to your install).

2. **Create the database the API expects**
   - In pgAdmin: right‑click **Databases** → **Create** → **Database**.
   - **Database**: `portal`
   - **Owner**: leave as `postgres` (or a user you will use in the connection string).
   - Save.

3. **Set the postgres user password (if needed)**
   - The API fallback uses `Username=postgres` and `Password=postgres`.
   - In pgAdmin: **Servers** → **PostgreSQL** → **Login/Group Roles** → right‑click **postgres** → **Properties** → **Definition** → set **Password** to `postgres` (or your choice).
   - If you use a different password, set it in the API (see **Configure the API** below).

4. **Configure the API**
   - Either add a connection string in `appsettings.Development.json` (see below), or use the default: `Host=localhost;Port=5432;Database=portal;Username=postgres;Password=postgres`.
   - Run the API: `dotnet run --project CargoHub.Api --urls "http://localhost:5000"`. On first run, EF Core will apply migrations and create tables.

---

## Option 3: Install PostgreSQL (Windows)

1. Download the installer: [PostgreSQL for Windows](https://www.postgresql.org/download/windows/) (e.g. from EDB).
2. Run the installer. When prompted:
   - Set a **password for the `postgres` user** (e.g. `postgres` so it matches the API fallback).
   - **Port**: leave **5432**.
3. Finish the install. You can install **pgAdmin** from the same installer (Stack Builder) or use the pgAdmin you already have.
4. Open **pgAdmin** and register the server:
   - **Host**: `localhost`
   - **Port**: `5432`
   - **Username**: `postgres`
   - **Password**: the one you set in the installer.
5. Create the database **portal** (right‑click **Databases** → **Create** → **Database**).
6. Run the API; it will use the default connection string if you didn’t override it.

---

## Configure the API (optional)

To override the connection string (e.g. different password or database name) without changing code, add to **CargoHub.Api/appsettings.Development.json**:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=portal;Username=postgres;Password=YOUR_PASSWORD_HERE"
  },
  "Logging": {
    "Default": "Information",
    "Microsoft.AspNetCore": "Warning"
  }
}
```

Do **not** commit real passwords to git. For local-only use, `appsettings.Development.json` is usually gitignored; if not, use [user-secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets):

```bash
cd CargoHub.Api
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=portal;Username=postgres;Password=YOUR_PASSWORD"
```

---

## Verify

- **Docker:** Run `.\run.ps1`, then in another terminal run `.\Scope\run-verify.ps1`.
- **Local PostgreSQL:** Start the API with `dotnet run --project CargoHub.Api --urls "http://localhost:5000"`, then run `.\Scope\run-verify.ps1`.
- In pgAdmin, connect to the server and open **Databases** → **portal** → **Schemas** → **public** → **Tables** to see the tables created by migrations.
