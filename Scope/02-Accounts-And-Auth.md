# Scope: Accounts and Auth

User account handling and all authentication flows (portal, plugin, integration, social). See [01-Architecture.md](01-Architecture.md) for the Accounts module structure.

## Portal auth (cookie + customer-id)

**Routes under api/v1/portal (public):**

- POST `/login` — body: `{ account, password }` (optional `type` query for token).
- POST `/register` — body: `{ email, password, userName, businessId, gsOne }`.
- POST `/requestPasswordReset`, POST `/resetPassword`, POST `/verify`, POST `/update-status` (verification).

**Behavior:**

- **Login**: Validate with ASP.NET Identity (email/password). Return:
  - `{ status: "OK", data: { websocket_token, type, userId, email, userName, verificationStatus, registrationDate, customerId, businessId } }`.
- Set **cookie** `X-Portal-Cookie` = AES-256-CBC encrypted value (e.g. userId or customerId), same options as booking-backend (httpOnly, secure, sameSite, maxAge).
- **customerId**: Stable id returned in response and required as header `customer-id` on protected requests. Use ApplicationUser.CustomerMappingId; set on register/login.
- **websocket_token**: Issue from local service (internalize user API getToken); return in `data.websocket_token`.

**Portal auth middleware** (protected api/v1/portal/* routes):

- Read cookie `X-Portal-Cookie`; decrypt with config key/iv (CRYPTO_KEY, CRYPTO_IV).
- Require header `customer-id`; validate it matches decrypted value (or mapped user).
- Optionally refresh cookie on each request.

Implement cookie encrypt/decrypt in .NET to match Node (SHA512-derived key/iv, AES-256-CBC, Base64).

## Standard login and portal path

- Standard login lives at `/api/v1/portal/login` for the portal. Current `api/v1/auth` can remain for other clients or be deprecated; if both exist, both should return a shape usable by the portal when calling portal login.

## Social login (Google, Facebook)

- Add ASP.NET Core OAuth (AddGoogle, AddFacebook). Callback: create or link ApplicationUser by email; set CustomerMappingId and required claims.
- **Portal compatibility**: After OAuth callback, issue same `X-Portal-Cookie` and return same `data` shape as login (e.g. redirect or JSON so portal can store session/customer-id).
- CORS: Allow portal origin, credentials, headers `customer-id`, `X-Portal-Cookie`.

## Plugin auth

- POST `/api/v1/plugin/login` — body includes `authentication_code` (or similar). Internalize: validate code (DB/config), resolve to customer/user, issue JWT with claims `iss: "CargoHub"`, `plugin-id`, `customer-id`. Return same response shape as current plugin login. Use existing JwtTokenFactory or Accounts token service.

## Integration auth

- POST `/api/v1/integration/auth` — body with integration credentials. Internalize lookup (e.g. integration_username / integration_password or token), resolve to customer_mapping_ID, issue JWT with `iss: "CargoHub"` and customer id. Use same JWT validation as plugin (Bearer) for integration routes.

JWT issuer/audience/signing key must match existing config for plugin/integration clients.

## User API operations (no external HTTP)

- **Verification**: POST `/api/v1/portal/verify`, `/update-status` — email verification status in Identity or custom table.
- **Password reset**: requestPasswordReset (create token, send email), resetPassword (validate token, update password via Identity).
- **Sub-users / user CRUD**: Expose as GET `/api/v1/portal/subusers/:id` etc. to match portal routes. Use Identity and any extra fields/tables for CustomerMappingId, sub-user links, verification status.
