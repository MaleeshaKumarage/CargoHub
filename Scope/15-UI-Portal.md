# Scope: Portal UI — same repo, separate deploy

The **portal UI** lives in the **`portal/`** folder at repo root and uses the tech stack and page scope defined in **[Scope/UI/](UI/)**.

## Principles

- **Same repo**: The UI project is in `portal/` (Next.js 15, TypeScript, Tailwind, shadcn/ui). Backend and portal are in one repo for development and versioning.
- **Separate deployment**: Backend and UI **deploy independently**. The API deploys as its own service; the UI deploys as a static site or SPA host (e.g. Vercel, CDN, separate container).
- **Connect via API only**: The UI talks to the backend **only over HTTP(S)** using `/api/v1/portal/*` (and company, bookings, etc.). Auth: JWT in `Authorization` header and `customer-id` header. No direct database or in-process calls.

## Tech stack (see [UI/README.md](UI/README.md))

- Next.js 15 (App Router), React 18, TypeScript, Tailwind CSS, **shadcn/ui**
- **Theming**: next-themes (light / dark / system) — [UI/13-Theming.md](UI/13-Theming.md)
- **i18n**: next-intl (fi, en, extensible) — [UI/14-i18n.md](UI/14-i18n.md)

## Page scope (one file per page)

| Scope doc | Page |
|-----------|------|
| [UI/01-Setup.md](UI/01-Setup.md) | Scaffold, env, CORS, run |
| [UI/02-Login.md](UI/02-Login.md) | Login |
| [UI/03-Register.md](UI/03-Register.md) | Register |
| [UI/04-ForgotPassword.md](UI/04-ForgotPassword.md) | Forgot password |
| [UI/05-ResetPassword.md](UI/05-ResetPassword.md) | Reset password |
| [UI/06-Home.md](UI/06-Home.md) | Home (protected) |
| [UI/07-Bookings.md](UI/07-Bookings.md) | Bookings list |
| [UI/08-BookingsCreate.md](UI/08-BookingsCreate.md) | Create booking |
| [UI/09-BookingsDetail.md](UI/09-BookingsDetail.md) | Booking detail |
| [UI/10-Actions.md](UI/10-Actions.md) | Actions |
| [UI/11-Plugin.md](UI/11-Plugin.md) | Plugin |
| [UI/12-More.md](UI/12-More.md) | More / settings |

## Implications

- **CORS**: Backend must allow the portal origin (e.g. `http://localhost:3000`) and credentials (see [02-Accounts-And-Auth.md](02-Accounts-And-Auth.md)).
- **Configuration**: UI uses **`NEXT_PUBLIC_API_URL`** (e.g. `https://localhost:5299`) pointing at the API. Different per environment (dev, staging, prod).
- **Branding**: The portal fetches company name, logo, and theme colors from **GET /api/v1/portal/branding** (no auth) and applies them in the navbar, document title, and CSS theme variables. See [16-Branding-And-Deployment.md](16-Branding-And-Deployment.md).
- When adding the UI: create `portal/` per [UI/01-Setup.md](UI/01-Setup.md); keep separate CI/CD so each deploys to its own target.
