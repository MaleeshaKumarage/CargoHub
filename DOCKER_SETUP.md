# Docker Hub Auto-Push Setup

When you push to `main`, GitHub Actions builds and pushes images to Docker Hub. Anyone can then pull and run with one command.

## One-time setup

### 1. Create Docker Hub repository

1. Go to [hub.docker.com](https://hub.docker.com) and sign in
2. Create one repository (public): `cargohub`

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

**One pull, one run:**
```bash
docker pull maleesha404/cargohub:latest
docker run -d -p 3000:3000 -p 8080:8080 -v cargohub_data:/var/lib/postgresql/data maleesha404/cargohub:latest
```

Image: [maleesha404/cargohub](https://hub.docker.com/r/maleesha404/cargohub)
