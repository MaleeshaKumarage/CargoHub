# Docker Hub Auto-Push Setup

When you push to `main`, GitHub Actions builds and pushes images to Docker Hub. Anyone can then pull and run with one command.

## One-time setup

### 1. Create Docker Hub repositories

1. Go to [hub.docker.com](https://hub.docker.com) and sign in
2. Create three repositories (public):
   - `cargohub` (all-in-one: db + api + portal)
   - `cargohub-api`
   - `cargohub-portal`

### 2. Create a Docker Hub access token

1. Docker Hub → Account Settings → Security → New Access Token
2. Name it (e.g. `github-actions`)
3. Permissions: **Read, Write, Delete**
4. Copy the token (you won't see it again)

### 3. Add GitHub Secrets

1. Your repo → **Settings** → **Secrets and variables** → **Actions**
2. **New repository secret** for each:

| Name | Value |
|------|-------|
| `DOCKERHUB_USERNAME` | Your Docker Hub username |
| `DOCKERHUB_TOKEN` | The access token from step 2 |

### 4. Done

Push to `main` and the workflow will build and push. Check **Actions** tab for status.

---

## Sharing with others

**One package (simplest):**
```bash
docker run -d -p 3000:3000 -p 8080:8080 -v cargohub_data:/var/lib/postgresql/data maleesha404/cargohub:latest
```

**Or with compose:**
```bash
docker compose -f docker-compose.one.yml up -d
```

Images: [maleesha404/cargohub](https://hub.docker.com/r/maleesha404/cargohub) (all-in-one) | [cargohub-api](https://hub.docker.com/r/maleesha404/cargohub-api) | [cargohub-portal](https://hub.docker.com/r/maleesha404/cargohub-portal)
