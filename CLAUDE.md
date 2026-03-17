# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

HiavaNet is a .NET 8 backend API with a Next.js 16 portal frontend. It follows Clean Architecture with CQRS using MediatR.

**Projects:**
- `HiavaNet.Api` - ASP.NET Core Web API (entry point)
- `HiavaNet.Application` - Business logic, MediatR handlers, DTOs
- `HiavaNet.Domain` - Domain entities (Booking, Company, etc.)
- `HiavaNet.Infrastructure` - EF Core, Identity, external services
- `HiavaNet.Launcher` - Dev launcher that starts API + Portal together
- `HiavaNet.Tests` - xUnit tests
- `portal/` - Next.js 16 frontend with React 19, Tailwind CSS 4, shadcn/ui

## Common Commands

### Backend (.NET)

```bash
# Build solution (stops running processes first to avoid file locks)
./build.ps1

# Run API with PostgreSQL (starts Docker DB, applies migrations)
./run.ps1

# Run only API (requires PostgreSQL running via Docker)
dotnet run --project HiavaNet.Api

# Build
dotnet build

# Run tests
dotnet test

# Run specific test
dotnet test --filter "FullyQualifiedName~TestClassName"

# Add EF Core migration (API must be stopped)
./add-migration.ps1
# Or manually:
dotnet ef migrations add <Name> -p HiavaNet.Infrastructure -s HiavaNet.Api
```

### Frontend (Portal)

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

- `ApplicationDbContext` in `HiavaNet.Infrastructure/Persistence/`
- Migrations are in `HiavaNet.Infrastructure/Migrations/`
- Migrations apply automatically on startup (`db.Database.Migrate()`)
- Critical schema ensured via `db.EnsureCriticalSchema()` for idempotency

### Authentication & Authorization

- JWT tokens with configurable Issuer/Audience/SigningKey
- Roles: SuperAdmin, Admin, User (created automatically on startup)
- Bootstrap secret required for creating first SuperAdmin
- Password reset and email verification via token stores

## Development Workflow

### Running Locally

**Option 1: Visual Studio (Recommended)**
1. Open `HiavaNet.Backend.sln`
2. Right-click `HiavaNet.Launcher` → Set as Startup Project
3. Press F5
4. API runs at http://localhost:5299, Portal at http://localhost:3000

**Option 2: CLI**
```bash
./run.ps1        # Starts DB + API
npm run dev      # In portal/ directory for UI
```

### Project Startup Configuration

- `HiavaNet.Launcher` - Runs both API and Portal (Development only)
- `HiavaNet.Api` - Runs API only (use for API-only debugging)

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

**Environment Variables:**
- `ASPNETCORE_ENVIRONMENT` - Development/Production
- `NEXT_PUBLIC_API_URL` - Portal API URL (portal/.env.local)

## Important Notes

- **Port 5433** is used for PostgreSQL to avoid conflicts with local PostgreSQL on 5432
- **build.ps1** stops running processes before building to avoid "file is locked" errors
- Portal auto-starts from API in Development mode if found at solution root
- Docker Desktop must be running for local database
