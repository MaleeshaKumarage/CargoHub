# CargoHub.Backend — Scope (future use)

This folder holds the scope for the .NET conversion of **booking-backend**, with internalized **CargoHub-userapi** and compatibility with **CargoHub-portal**. Each function/feature has its own scope document.

## Overview

- **Goal**: Full REST API compatibility with booking-backend; endpoints unchanged so the portal and other clients work without change. The app is de-branded and configurable per deployment (company name, logo, colors) — see [16-Branding-And-Deployment.md](16-Branding-And-Deployment.md).
- **Auth**: .NET standard login + social login (Google, Facebook); all account logic internal (no external user API).
- **Architecture**: Clean Architecture + CQRS + MediatR; user account handling in a separated **Accounts** module.
- **Future UI**: Room is reserved for a rebuilt UI (portal) project inside this repo; backend and UI **deploy separately** and **connect only via the API** (see [15-UI-Portal.md](15-UI-Portal.md)).

## Scope documents (by feature)

| Document | Content |
|----------|---------|
| [01-Architecture.md](01-Architecture.md) | Clean Architecture, CQRS, MediatR, Accounts module structure |
| [02-Accounts-And-Auth.md](02-Accounts-And-Auth.md) | Portal auth, login/register, password reset, verify, social login, plugin/integration auth |
| [03-Portal.md](03-Portal.md) | Portal feature — all portal endpoints and behavior |
| [04-Company.md](04-Company.md) | Company feature — address book, default-shipper, configurations, agreement-numbers |
| [04-Company-And-Registration.md](04-Company-And-Registration.md) | Company model (admin-created), government ID (BusinessId), user registration with company ID; dashboard first page |
| [05-Bookings.md](05-Bookings.md) | Bookings feature — portal bookings, drafts, booking route group |
| [06-Plugin.md](06-Plugin.md) | Plugin feature — login, silent-login, bookings, custom-mapping |
| [07-Integration.md](07-Integration.md) | Integration feature — auth, bookings, updates |
| [08-Dashboard.md](08-Dashboard.md) | Dashboard feature — totals and averages |
| [09-Token.md](09-Token.md) | Token feature — internal token with cargohub-header |
| [10-PickupPoint.md](10-PickupPoint.md) | Pickup point feature — carrier pickup points |
| [11-Transport.md](11-Transport.md) | Transport (Youredi) — status updates and booking updates |
| [12-Health-And-Swagger.md](12-Health-And-Swagger.md) | Health check and Swagger/OpenAPI |
| [13-Implementation-Layout.md](13-Implementation-Layout.md) | Files to add/change, controller layout, phasing |
| [14-Full-Parity-Checklist.md](14-Full-Parity-Checklist.md) | Complete booking-backend route list for feature parity |
| [15-UI-Portal.md](15-UI-Portal.md) | Portal UI: same repo, separate deploy; connect via API only; tech stack and page index |
| [16-Branding-And-Deployment.md](16-Branding-And-Deployment.md) | Company branding: configurable app name, logo, colors per deployment; de-branded user-facing surfaces; GET /api/v1/portal/branding |
| **[UI/](UI/)** | **Portal UI scope: one doc per page** — setup, login, register, forgot/reset password, home, bookings (list/create/detail), actions, plugin, more, theming, i18n (see [UI/README.md](UI/README.md)) |

## Deployment model

- **Backend**: Deploys as its own service (API only). Does not serve the UI.
- **UI (when added)**: Can live in this repo but deploys separately (e.g. static site / SPA host). Connects to the backend via the same `/api/v1` REST API. CORS and API base URL config must support this.

## Phasing

- **Phase 1** — Auth and portal entry: Health, portal auth (login, register, password reset, verify, update-status), cookie + customer-id middleware, plugin/integration auth (JWT), social login.
- **Phase 2** — Portal and company: Every portal protected route and every company route from the checklist; booking route group.
- **Phase 3** — Remaining: Plugin, integration, dashboard, token, pickuppoint, transport; Swagger/OpenAPI.

## Reference

- **booking-backend**: Node/Express source — `C:\Users\malee\source\repos\booking-backend`
- **CargoHub-userapi**: Java (accounts) + Python (API) — logic to be internalized
- **CargoHub-portal**: Frontend — must work without change against this backend
