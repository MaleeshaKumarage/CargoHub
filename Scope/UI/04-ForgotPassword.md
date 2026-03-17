# UI: Forgot password (request reset)

## Route

- **Path**: `[locale]/(auth)/forgot-password`.
- **Group**: Public.

## Behaviour

- Form: **email**.
- Submit: `POST {NEXT_PUBLIC_API_URL}/api/v1/portal/requestPasswordReset` with body `{ email }`.
- Success: Show generic success message (do not reveal whether the email exists). Optionally link to login or reset-password.
- Failure: Can still show generic message or API message depending on product choice.

## Components

- shadcn Input, Button; link back to login.
- Copy from i18n.
