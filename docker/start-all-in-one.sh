#!/bin/sh
set -e

# PostgreSQL must run as postgres user (root execution is not permitted)
export PGDATA="${PGDATA:-/var/lib/postgresql/data}"
mkdir -p "$PGDATA"
chown postgres:postgres "$PGDATA" 2>/dev/null || true

# Init database if empty
if [ ! -f "$PGDATA/PG_VERSION" ]; then
  gosu postgres initdb -D "$PGDATA"
  echo "host all all 0.0.0.0/0 md5" >> "$PGDATA/pg_hba.conf"
  echo "listen_addresses='*'" >> "$PGDATA/postgresql.conf"
fi

# Start PostgreSQL as postgres user (gosu required - postgres refuses to run as root)
gosu postgres postgres -D "$PGDATA" &

# Wait for PostgreSQL to be ready
until pg_isready -U postgres 2>/dev/null; do
  sleep 1
done

# Create portal database if missing
gosu postgres psql -tc "SELECT 1 FROM pg_database WHERE datname = 'portal'" | grep -q 1 || gosu postgres createdb portal

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

# Optional: ngrok inside the container (set NGROK_AUTHTOKEN at docker run / compose)
# Public URLs: open http://localhost:4040 on the host when port 4040 is published, or check container logs
if [ -n "$NGROK_AUTHTOKEN" ]; then
  echo "Starting ngrok (in-container)..."
  ngrok config add-authtoken "$NGROK_AUTHTOKEN" 2>/dev/null || true
  ngrok start --all --config /etc/ngrok/ngrok.yml >> /tmp/ngrok.log 2>&1 &
  sleep 3
  echo "ngrok tunnel status:"
  curl -sS "http://127.0.0.1:4040/api/tunnels" 2>/dev/null | head -c 4000 || echo "(ngrok API not ready yet — check /tmp/ngrok.log)"
  echo ""
fi

# Start Portal (foreground - keeps container alive)
# HOSTNAME=0.0.0.0 required so Next.js accepts connections from outside container
cd /app/portal
export NODE_ENV=production
export PORT=3000
export HOSTNAME=0.0.0.0
exec npm run start
