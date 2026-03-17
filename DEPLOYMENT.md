# CargoHub Free Demo Deployment Guide

This guide walks you through deploying CargoHub **for free** with zero prior experience. You'll use:

- **Neon** – Free PostgreSQL database
- **Render** – Free hosting for the .NET API
- **Vercel** – Free hosting for the Next.js portal

**Total cost: $0**

---

## Prerequisites

1. **GitHub account** – [github.com](https://github.com) (free)
2. **Git** – Push your code to GitHub (or use GitHub Desktop)

---

## Part 1: Push Your Code to GitHub

1. Create a new repository on GitHub: [github.com/new](https://github.com/new)
   - Name it something like `cargohub-demo`
   - Choose **Public**
   - Do **not** initialize with README (you already have code)

2. Push your local code:

   ```powershell
   cd c:\Users\malee\source\HiavaNet.Backend
   git remote add origin https://github.com/YOUR_USERNAME/cargohub-demo.git
   git branch -M main
   git push -u origin main
   ```

   Replace `YOUR_USERNAME` with your GitHub username.

---

## Part 2: Create the Database (Neon)

1. Go to [neon.tech](https://neon.tech) and sign in with **GitHub**.

2. Click **"New Project"**.

3. Fill in:
   - **Project name:** `cargohub-demo`
   - **Region:** Choose closest to you (e.g. US East)
   - **PostgreSQL version:** 16 (default)

4. Click **"Create project"**.

5. On the project dashboard, find the **Connection string**.
   - Click **"Connection string"** or the copy icon.
   - Copy the **pooled** connection string (starts with `postgresql://`).
   - Example: `postgresql://user:password@ep-xxx.us-east-2.aws.neon.tech/neondb?sslmode=require`

6. **Save this string** – you'll need it for Render.

---

## Part 3: Deploy the API (Render)

1. Go to [render.com](https://render.com) and sign in with **GitHub**.

2. Click **"New +"** → **"Web Service"**.

3. Connect your repository:
   - If prompted, click **"Connect account"** to authorize Render.
   - Select your `cargohub-demo` (or your repo name) repository.
   - Click **"Connect"**.

4. Configure the service:

   | Field | Value |
   |-------|-------|
   | **Name** | `cargohub-api` (or any name) |
   | **Region** | Choose closest to you |
   | **Branch** | `main` |
   | **Runtime** | **Docker** |
   | **Dockerfile Path** | `Dockerfile` (leave default) |
   | **Instance Type** | **Free** |

5. Click **"Advanced"** and add these **Environment Variables** (or use [GitHub Secrets](SECRETS.md) if deploying via Actions):

   | Key | Value |
   |----|-------|
   | `ASPNETCORE_ENVIRONMENT` | `Production` |
   | `ConnectionStrings__DefaultConnection` | *(paste your Neon connection string from Part 2)* |
   | `Bootstrap__Secret` | *(choose a secret, e.g. `MyDemoBootstrapSecret123`)* |
   | `Jwt__SigningKey` | *(choose a long random string, e.g. `DemoJwtKey-ChangeMe-AtLeast32Chars!`)* |

   **Important:** For `Cors__PortalOrigin`, add it **after** you deploy the portal (Part 4). You'll come back and add:
   - `Cors__PortalOrigin` = `https://your-portal-name.vercel.app` (your Vercel URL)

6. Click **"Create Web Service"**.

7. Wait 5–10 minutes for the first build. When it says **"Live"**, your API is running.

8. Copy your API URL (e.g. `https://cargohub-api.onrender.com`) – you'll need it for Vercel.

**Note:** On the free tier, the API sleeps after 15 minutes of no traffic. The first request after that may take ~1 minute to wake up.

---

## Part 4: Deploy the Portal (Vercel)

1. Go to [vercel.com](https://vercel.com) and sign in with **GitHub**.

2. Click **"Add New..."** → **"Project"**.

3. Import your repository:
   - Select your `cargohub-demo` repo.
   - Click **"Import"**.

4. Configure the project:

   | Field | Value |
   |-------|-------|
   | **Framework Preset** | Next.js (auto-detected) |
   | **Root Directory** | Click **"Edit"** and set to `portal` |
   | **Build Command** | `npm run build` (default) |
   | **Output Directory** | `.next` (default) |

5. Add **Environment Variable**:
   - **Name:** `NEXT_PUBLIC_API_URL`
   - **Value:** `https://YOUR-RENDER-URL.onrender.com` (from Part 3, no trailing slash)

6. Click **"Deploy"**.

7. Wait 2–3 minutes. When done, you'll get a URL like `https://cargohub-demo-xxx.vercel.app`.

---

## Part 5: Connect API and Portal (CORS)

1. Go back to **Render** → your API service → **Environment** tab.

2. Add a new variable:
   - **Key:** `Cors__PortalOrigin`
   - **Value:** `https://your-actual-vercel-url.vercel.app` (from Part 4, no trailing slash)

3. Click **"Save Changes"**. Render will redeploy automatically (takes a few minutes).

---

## Part 6: Create Your First Admin User

1. Open your **portal URL** (from Vercel) in a browser.

2. You'll need to create the first SuperAdmin. Use a tool like Postman, or run this in PowerShell:

   ```powershell
   $apiUrl = "https://YOUR-RENDER-URL.onrender.com"
   $bootstrapSecret = "MyDemoBootstrapSecret123"  # Same as Bootstrap__Secret in Render

   $body = @{
       email = "admin@example.com"
       password = "Admin123!"
       fullName = "Demo Admin"
   } | ConvertTo-Json

   Invoke-RestMethod -Uri "$apiUrl/api/v1/portal/bootstrap-superadmin" `
       -Method Post `
       -ContentType "application/json" `
       -Headers @{ "X-Bootstrap-Secret" = $bootstrapSecret } `
       -Body $body
   ```

   Replace `YOUR-RENDER-URL` and `MyDemoBootstrapSecret123` with your values.

3. After success, go to your portal and log in with `admin@example.com` / `Admin123!`.

---

## Summary of URLs

| What | URL |
|------|-----|
| **Portal** | `https://your-project.vercel.app` |
| **API** | `https://cargohub-api.onrender.com` |
| **Database** | Managed by Neon (no direct URL needed) |

---

## Troubleshooting

### "Failed to fetch" or CORS errors
- Ensure `Cors__PortalOrigin` in Render exactly matches your Vercel URL (no trailing slash).
- Redeploy the API after adding the variable.

### API takes a long time to respond
- Free tier sleeps after 15 min. First request may take ~1 minute. Subsequent requests are fast.

### "Bootstrap is not configured"
- Add `Bootstrap__Secret` in Render environment variables.
- Use the same value in the `X-Bootstrap-Secret` header when calling the bootstrap endpoint.

### Database connection errors
- Check the Neon connection string. Use the **pooled** connection string.
- Ensure `sslmode=require` is in the connection string (Neon requires SSL).

---

## Updating Your Demo

When you push changes to GitHub:

- **Render** automatically redeploys the API.
- **Vercel** automatically redeploys the portal.

No manual steps needed.
