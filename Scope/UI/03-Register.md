# UI: Register page

## Route

- **Path**: `[locale]/(auth)/register`.
- **Group**: Public.

## Behaviour

- Form: email, password, userName, optional businessId, gsOne.
- Submit: `POST {NEXT_PUBLIC_API_URL}/api/v1/portal/register` with body `{ email, password, userName, businessId?, gsOne? }`.
- Success: API returns 200 and login-shaped response (userId, email, displayName, jwtToken, etc.). Store token/user; redirect to home.
- Failure: Show API error (e.g. validation or "Failed to register user").

## Components

- shadcn form components; validation (e.g. zod) for required fields and password rules.
- Link to login.
- Copy from i18n.
