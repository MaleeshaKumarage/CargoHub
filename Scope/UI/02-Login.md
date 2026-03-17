# UI: Login page

## Route

- **Path**: `[locale]/(auth)/login` (e.g. `/fi/login`, `/en/login`).
- **Group**: Public (no auth required).

## Behaviour

- Form: **account** (email, username, or display name), **password**.
- Submit: `POST {NEXT_PUBLIC_API_URL}/api/v1/portal/login` with body `{ account, password }`.
- Success: API returns 200 and `{ userId, email, displayName, businessId, customerMappingId, jwtToken }`. Store token and user (e.g. AuthContext); optionally store `customerId` for the `customer-id` header. Redirect to home (`/[locale]/home` or default).
- Failure: API returns 401 with `{ errorCode, message }`. Show message (e.g. "Invalid email/username or password.").

## Components

- Use shadcn `Input`, `Button`, `Card`; form with react-hook-form + zod if desired.
- Links to register and forgot-password.
- All copy from i18n (e.g. `useTranslations('auth')`).
