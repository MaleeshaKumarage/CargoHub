# Creating the first Super Admin

The **first Super Admin** is not created via the normal portal registration. It is created once via a **bootstrap endpoint** that is protected by a secret. This avoids anyone being able to self-register as Super Admin.

---

## 1. Set the bootstrap secret

The API must have **`Bootstrap:Secret`** in configuration. If it is missing, the bootstrap endpoint returns 500.

- **Development:** In `CargoHub.Api/appsettings.Development.json` there is a placeholder:
  ```json
  "Bootstrap": {
    "Secret": "change-me-in-production-use-user-secrets-or-env"
  }
  ```
  You can keep this for local use or set a stronger value.

- **Production:** Do **not** put the real secret in appsettings. Use:
  - **User secrets:**  
    `dotnet user-secrets set "Bootstrap:Secret" "your-strong-secret" --project CargoHub.Api`
  - Or an **environment variable:**  
    `Bootstrap__Secret=your-strong-secret`

---

## 2. Call the bootstrap endpoint once

- **URL:** `POST /api/v1/portal/bootstrap-superadmin`
- **Header:** `X-Bootstrap-Secret: <value of Bootstrap:Secret>`
- **Body (JSON):**
  ```json
  {
    "email": "admin@example.com",
    "password": "YourSecurePassword123",
    "displayName": "Super Admin"
  }
  ```
  - `email` and `password` are required; `password` must be at least 6 characters.
  - `displayName` is optional; defaults to the email.

**Example (PowerShell, API base URL `http://localhost:5299`):**

```powershell
$secret = "change-me-in-production-use-user-secrets-or-env"   # same as in config
$body = @{ email = "admin@example.com"; password = "YourSecurePassword123"; displayName = "Super Admin" } | ConvertTo-Json
Invoke-RestMethod -Uri "http://localhost:5299/api/v1/portal/bootstrap-superadmin" -Method Post -Headers @{ "Content-Type" = "application/json"; "X-Bootstrap-Secret" = $secret } -Body $body
```

**Example (curl):**

```bash
curl -X POST "http://localhost:5299/api/v1/portal/bootstrap-superadmin" \
  -H "Content-Type: application/json" \
  -H "X-Bootstrap-Secret: <your Bootstrap:Secret from config>" \
  -d '{"email":"admin@example.com","password":"YourSecurePassword123","displayName":"Super Admin"}'
```

---

## 3. After bootstrap

- If the request succeeds, you get a **201** response and a message that the Super Admin was created.
- **Log in to the portal** with that email and password. The Super Admin role is already assigned.
- The bootstrap endpoint **can only run once**: if any user already has the Super Admin role, it returns **400** and does not create another account.
- Change the password after first login if you used a temporary one.
