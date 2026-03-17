# Scope: Portal UI

Portal frontend lives in **`portal/`** at repo root (Next.js 15, TypeScript, Tailwind, shadcn/ui). Same repo as backend; separate deploy. Connects to the API via `NEXT_PUBLIC_API_URL` (e.g. `https://localhost:5299`) and uses `/api/v1/portal/*` endpoints.

## Stack

- **Framework**: Next.js 15 (App Router), React 18, TypeScript
- **UI**: Tailwind CSS, shadcn/ui
- **Theme**: next-themes (light / dark / system), persisted
- **i18n**: next-intl (fi, en; extensible)
- **Auth**: JWT + `customer-id` header; AuthContext or small store

## Page scope documents

| File | Page / topic |
|------|----------------|
| [01-Setup.md](01-Setup.md) | Scaffold, env, CORS, run instructions |
| [02-Login.md](02-Login.md) | Login page and API |
| [03-Register.md](03-Register.md) | Register page and API |
| [04-ForgotPassword.md](04-ForgotPassword.md) | Request password reset |
| [05-ResetPassword.md](05-ResetPassword.md) | Reset password with token |
| [06-Home.md](06-Home.md) | Protected home / dashboard entry |
| [07-Bookings.md](07-Bookings.md) | Bookings list |
| [08-BookingsCreate.md](08-BookingsCreate.md) | Create booking form |
| [09-BookingsDetail.md](09-BookingsDetail.md) | Single booking detail |
| [10-Actions.md](10-Actions.md) | Actions (agreement numbers, shipper, etc.) |
| [11-Plugin.md](11-Plugin.md) | Plugin download / release notes |
| [12-More.md](12-More.md) | More / settings |
| [13-Theming.md](13-Theming.md) | Light / dark / system theme |
| [14-i18n.md](14-i18n.md) | Multi-language (fi, en, adding locales) |

## Principles (from 15-UI-Portal.md)

- Same repo, separate deployment; UI and API deploy independently.
- Connect via API only (no direct DB or in-process calls).
- CORS must allow the portal origin and credentials; API base URL configurable per environment.
