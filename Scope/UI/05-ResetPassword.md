# UI: Reset password

## Route

- **Path**: `[locale]/(auth)/reset-password` (token may be query param, e.g. `?token=...`).
- **Group**: Public.

## Behaviour

- Form: **token** (pre-filled from URL if present), **newPassword** (and confirm).
- Submit: `POST {NEXT_PUBLIC_API_URL}/api/v1/portal/resetPassword` with body `{ token, newPassword }`.
- Success: Show success; redirect to login.
- Failure: Show API message (e.g. "Invalid or expired token.").

## Components

- shadcn form; link to login.
- Copy from i18n.
