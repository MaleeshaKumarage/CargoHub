# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

CargoHub is a .NET 8 backend API with a Next.js 16 portal frontend. It follows Clean Architecture with CQRS using MediatR.

**Projects:**
- `CargoHub.Api` - ASP.NET Core Web API (entry point)
- `CargoHub.Application` - Business logic, MediatR handlers, DTOs
- `CargoHub.Domain` - Domain entities (Booking, Company, etc.)
- `CargoHub.Infrastructure` - EF Core, Identity, external services
- `CargoHub.Launcher` - Dev launcher that starts API + Portal together
- `CargoHub.Tests` - xUnit tests
- `portal/` - Next.js 16 frontend with React 19, Tailwind CSS 4, shadcn/ui

## Common Commands

### Backend (.NET)

```bash
# Build solution (stops running processes first to avoid file locks)
./build.ps1

# Run API with PostgreSQL (starts Docker DB, applies migrations)
./run.ps1

# Run only API (requires PostgreSQL running via Docker)
dotnet run --project CargoHub.Api

# Build
dotnet build

# Run tests
dotnet test

# Run specific test
dotnet test --filter "FullyQualifiedName~TestClassName"

# Add EF Core migration (API must be stopped)
./add-migration.ps1
# Or manually:
dotnet ef migrations add <Name> -p CargoHub.Infrastructure -s CargoHub.Api
```

### Frontend (Portal)

Use **Node.js** from `portal/.nvmrc` (e.g. `nvm use` / `fnm use` in `portal/`) so local versions match CI. **Vitest** is pinned to **3.x** (Vite-based) for compatibility with Node 20.11+; Vitest 4+ pulls in rolldown which needs **Node ≥20.12** (`util.styleText`).

```bash
# Install dependencies
cd portal && npm install

# Dev server (runs on http://localhost:3000)
npm run dev

# Build
npm run build

# Lint
npm run lint

# Run tests (Vitest)
npm run test
npm run test:watch
```

### Database

```bash
# Start PostgreSQL in Docker
docker compose up -d

# Port: 5433 (host) -> 5432 (container) to avoid conflicts with local PostgreSQL
# Connection string defaults to: Host=localhost;Port=5433;Database=portal;Username=postgres;Password=postgres
```

## Architecture

### Clean Architecture / CQRS

**Layer Dependencies:**
- `Domain` - No dependencies (entities, value objects)
- `Application` -> `Domain` (commands, queries, handlers, DTOs, interfaces)
- `Infrastructure` -> `Application`, `Domain` (EF Core, Identity, repositories)
- `Api` -> `Application`, `Infrastructure`

**MediatR Pattern:**
- Commands in `Application/<Feature>/Commands/`
- Queries in `Application/<Feature>/Queries/`
- Handlers implement `IRequestHandler<TRequest, TResponse>`
- `AssemblyMarker` class used for MediatR registration

**Key Technologies:**
- **Database:** PostgreSQL with EF Core 8, Npgsql
- **Identity:** ASP.NET Core Identity with JWT Bearer authentication
- **PDF Generation:** QuestPDF
- **Validation:** FluentValidation

### Entity Framework

- `ApplicationDbContext` in `CargoHub.Infrastructure/Persistence/`
- Migrations are in `CargoHub.Infrastructure/Migrations/`
- Migrations apply automatically on startup (`db.Database.Migrate()`)
- Critical schema ensured via `db.EnsureCriticalSchema()` for idempotency

### Authentication & Authorization

- JWT tokens with configurable Issuer/Audience/SigningKey
- Roles: SuperAdmin, Admin, User (created automatically on startup)
- Bootstrap secret required for creating first SuperAdmin
- Password reset and email verification via token stores

## Development Workflow

### Git branches (required)

- **Do not push directly to `main` or `master`.** Land changes with **Pull Requests into `development`**, then promote with **PRs from `development` → `main` / `master`** (never `git push origin main` or `master` to ship work).
- Use feature/fix branches; avoid committing feature work directly on `development`, `main`, or `master`.

### Running Locally

**Option 1: Visual Studio (Recommended)**
1. Open `CargoHub.Backend.sln`
2. Right-click `CargoHub.Launcher` → Set as Startup Project
3. Press F5
4. API runs at http://localhost:5299, Portal at http://localhost:3000

**Option 2: CLI**
```bash
./run.ps1        # Starts DB + API
npm run dev      # In portal/ directory for UI
```

### Project Startup Configuration

- `CargoHub.Launcher` - Runs both API and Portal (Development only)
- `CargoHub.Api` - Runs API only (use for API-only debugging)

### Docker all-in-one + ngrok (remote demo)

- **CI:** `docker-build-push.yml` (Ubuntu build/push) → `docker-deploy-mac.yml` (`workflow_run`, self-hosted deploy — avoids queue deadlocks on one Mac runner).
- **Image:** `Dockerfile.all-in-one` — PostgreSQL + API + Next.js + **nginx** on **`:8888`** (single public entry: UI + `/api`).
- **ngrok:** `ngrok.yml` tunnels **one** address — **`:8888`** (not separate 3000/8080). In-container ngrok web UI is **`:4040`**; compose maps host **`:14040`** → **`:4040`** to avoid clashing with host ngrok on **`:4040`**.
- **Portal API URL:** Production image builds with `NEXT_PUBLIC_API_URL=__SAME_ORIGIN__`; `portal/src/lib/api.ts` uses `window.location.origin` in the browser so API calls match the ngrok URL.
- **CORS:** Set `Cors__PortalOrigin` / `CORS__PORTAL_ORIGIN` to the **HTTPS ngrok origin** (same as the public URL) when testing from the internet.
- **Routes:** Portal uses locale prefixes — e.g. **`/en/login`**, **`/en/dashboard`** — not bare `/dashboard`.
- **Docs:** See [RUN.md](RUN.md) and `docker-compose.one.yml`.

## Testing

- xUnit with Moq and NSubstitute
- Integration tests use EF Core InMemory provider
- `TestDbFixture` provides shared database context setup

## Key Configuration

**appsettings.Development.json:**
- `ConnectionStrings:DefaultConnection` - PostgreSQL connection
- `Jwt:*` - Token configuration
- `Cors:PortalOrigin` - Allowed CORS origins
- `Bootstrap:Secret` - SuperAdmin creation secret

**Environment Variables / Secrets:**
- `ASPNETCORE_ENVIRONMENT` - Development/Production
- `NEXT_PUBLIC_API_URL` - Portal API URL (portal/.env.local); Docker all-in-one build uses `__SAME_ORIGIN__` (see `api.ts`)
- `Cors:PortalOrigin` / `CORS__PORTAL_ORIGIN` - Must match the browser origin (e.g. ngrok HTTPS URL) for remote demos
- Secrets live in `.env` (copy from `.env.example`). See [SECRETS.md](SECRETS.md) for GitHub Secrets.

## Important Notes

- **Port 5433** is used for PostgreSQL to avoid conflicts with local PostgreSQL on 5432
- **Docker public port 8888** — nginx fronts Next (`:3000`) and API (`:8080`); use this for a single ngrok URL
- **build.ps1** stops running processes before building to avoid "file is locked" errors
- Portal auto-starts from API in Development mode if found at solution root
- Docker Desktop must be running for local database
