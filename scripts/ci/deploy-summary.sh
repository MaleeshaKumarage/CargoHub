#!/usr/bin/sh
# Append Mac deploy details to GitHub Actions Job Summary ($GITHUB_STEP_SUMMARY).
# Expects: GITHUB_STEP_SUMMARY, optional IMAGE_DIGEST, IMAGE_TAGS, COMPOSE_PROJECT (default cargohub).
set -e

SUMMARY="${GITHUB_STEP_SUMMARY:-/dev/stdout}"
COMPOSE_PROJECT="${COMPOSE_PROJECT:-cargohub}"

{
  echo "## Deployment report (self-hosted runner)"
  echo ""
  echo "### Runner"
  echo "\`\`\`"
  echo "hostname: $(hostname 2>/dev/null || echo n/a)"
  echo "date: $(date -u 2>/dev/null || date)"
  uname -a 2>/dev/null || true
  echo "\`\`\`"
  echo ""
  echo "### Docker"
  echo "\`\`\`"
  docker version --format '{{.Server.Version}}' 2>/dev/null && echo "(server)" || docker version 2>/dev/null | head -5 || echo "docker n/a"
  echo "\`\`\`"
  echo ""
  echo "### Image built in CI (this run)"
  echo "| | |"
  echo "|-|-|"
  if [ -n "${IMAGE_DIGEST:-}" ]; then
    echo "| **Manifest digest** (from build job) | \`${IMAGE_DIGEST}\` |"
  else
    echo "| **Manifest digest** | (not available from build job) |"
  fi
  echo ""
  echo "### Local image after pull"
  echo "\`\`\`"
  docker images --format "table {{.Repository}}\t{{.Tag}}\t{{.ID}}\t{{.CreatedSince}}\t{{.Size}}" 2>/dev/null | head -20 || echo "docker images n/a"
  echo "\`\`\`"
  echo ""
  echo "### Container (compose project / name: ${COMPOSE_PROJECT})"
  echo "\`\`\`"
  docker ps -a --filter "name=${COMPOSE_PROJECT}" --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}" 2>/dev/null || docker ps -a 2>/dev/null | head -15 || echo "docker ps n/a"
  echo "\`\`\`"
  echo ""
  if docker inspect "${COMPOSE_PROJECT}" >/dev/null 2>&1; then
    echo "### Inspect ${COMPOSE_PROJECT} (image + ports)"
    echo "\`\`\`json"
    docker inspect "${COMPOSE_PROJECT}" --format '{{json .Config.Image}}' 2>/dev/null || true
    docker inspect "${COMPOSE_PROJECT}" --format '{{range $k,$v := .NetworkSettings.Ports}}{{$k}} -> {{json $v}}{{"\n"}}{{end}}' 2>/dev/null || true
    echo "\`\`\`"
  fi
  echo ""
  echo "### Endpoints verified by smoke tests"
  echo "| Service | URL |"
  echo "|---------|-----|"
  echo "| API | http://localhost:8080/api/v1/health_ |"
  echo "| Portal (direct) | http://localhost:3000/ |"
  echo "| Public (nginx) | http://localhost:8888/en/ |"
} >> "$SUMMARY"

echo "Wrote deployment report to GitHub Actions Summary."
