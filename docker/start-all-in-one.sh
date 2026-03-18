#!/bin/sh
set -e

# Start PostgreSQL in background (uses postgres image's entrypoint)
/usr/local/bin/docker-entrypoint.sh postgres &
PGPID=$!

# Wait for PostgreSQL to be ready
until pg_isready -U postgres -d portal 2>/dev/null; do
  sleep 1
done

# Start .NET API in background
export ASPNETCORE_ENVIRONMENT=Production
export PORT=8080
export ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=portal;Username=postgres;Password=postgres"
export Cors__PortalOrigin="http://localhost:3000"
export Bootstrap__Secret="${Bootstrap__Secret:-SuperAdminBootstrapSecret}"
export Jwt__SigningKey="${Jwt__SigningKey:-DemoJwtKey-ChangeMe-AtLeast32Chars}"
cd /app/api && dotnet CargoHub.Api.dll &
APIPID=$!

# Give API time to start and run migrations
sleep 5

# Start Portal (foreground - keeps container alive)
# HOSTNAME=0.0.0.0 required so Next.js accepts connections from outside container
cd /app/portal
export NODE_ENV=production
export PORT=3000
export HOSTNAME=0.0.0.0
exec npm run start
