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
export Cors__PortalOrigin="${Cors__PortalOrigin:-http://localhost:3000}"
export Bootstrap__Secret="${Bootstrap__Secret:-SuperAdminBootstrapSecret}"
export Jwt__SigningKey="${Jwt__SigningKey:-DemoJwtKey-ChangeMe-AtLeast32Chars}"
cd /app/api && dotnet CargoHub.Api.dll &
APIPID=$!

# Program.cs runs Migrate() before Kestrel listens; on slow IO this can exceed 10m. Match host smoke retries (~15m).
API_WAIT_MAX="${CARGOHUB_API_WAIT_MAX:-900}"
echo "Waiting for API on :8080 (migrations may run on first start, max ${API_WAIT_MAX}s)..."
i=0
while [ "$i" -lt "$API_WAIT_MAX" ]; do
  if curl -sfS "http://127.0.0.1:8080/api/v1/health_" >/dev/null 2>&1; then
    echo "API is up."
    break
  fi
  i=$((i + 1))
  sleep 1
done
if [ "$i" -ge "$API_WAIT_MAX" ]; then
  echo "ERROR: API did not become ready on :8080 within ${API_WAIT_MAX}s"
  exit 1
fi

# Next.js on :3000 (nginx will expose :8888 to the internet via ngrok)
cd /app/portal
export NODE_ENV=production
export PORT=3000
export HOSTNAME=0.0.0.0
npm run start &
NEXT_PID=$!

NEXT_WAIT_MAX="${CARGOHUB_NEXT_WAIT_MAX:-240}"
echo "Waiting for Next.js on :3000 (locale routes use /en/...)..."
i=0
while [ "$i" -lt "$NEXT_WAIT_MAX" ]; do
  if curl -sfS -o /dev/null "http://127.0.0.1:3000/en/" 2>/dev/null; then
    echo "Next.js is up."
    break
  fi
  i=$((i + 1))
  sleep 1
done
if [ "$i" -ge "$NEXT_WAIT_MAX" ]; then
  echo "WARNING: Next.js did not respond on :3000 within ${NEXT_WAIT_MAX}s; continuing to start nginx"
fi

# Reverse proxy: one public port — /api -> ASP.NET :8080, / -> Next :3000
if nginx -t 2>/dev/null; then
  nginx
  echo "nginx listening on :8888 ( /api -> :8080, / -> :3000 )"
else
  echo "WARNING: nginx -t failed"; nginx -t || true
fi

# ngrok: single tunnel to :8888 (not 3000/8080) so browser has one origin for UI + API
if [ -n "$NGROK_AUTHTOKEN" ]; then
  echo "Starting ngrok (in-container) -> :8888..."
  ngrok config add-authtoken "$NGROK_AUTHTOKEN" 2>/dev/null || true
  ngrok start --all --config /etc/ngrok/ngrok.yml >> /tmp/ngrok.log 2>&1 &
  i=0
  while [ "$i" -lt 60 ]; do
    if curl -fsS "http://127.0.0.1:4040/api/tunnels" >/dev/null 2>&1; then
      echo "ngrok API is up."
      break
    fi
    i=$((i + 1))
    sleep 1
  done
  if [ "$i" -ge 60 ]; then
    echo "ngrok API did not become ready. Last 50 lines of /tmp/ngrok.log:"
    tail -50 /tmp/ngrok.log 2>/dev/null || true
  else
    echo "ngrok tunnel status:"
    curl -sS "http://127.0.0.1:4040/api/tunnels" 2>/dev/null | head -c 4000 || true
    echo ""
  fi
fi

echo "Public demo: use ngrok HTTPS URL on port 8888 (nginx). Paths: /en/login, /en/dashboard — not /dashboard alone."
wait "$NEXT_PID"
