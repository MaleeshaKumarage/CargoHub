# Scope: Bookings

Booking endpoints from two surfaces: (1) **Portal** bookings under `/api/v1/portal` (see [03-Portal.md](03-Portal.md)), and (2) **Booking route group** under `/api/v1/booking` with mixed auth. Implement via CQRS/MediatR.

## Booking route group (`/api/v1/booking`)

Exact paths and auth as in booking-backend (booking.route.ts). Note: backend mounts at `api/v1/booking` and uses paths like `/api/v1/bookings` and `/custom/create`; ensure full URLs match how portal and plugin clients call them.

- POST `/api/v1/bookings` — create booking — **PortalAuthMiddleware**
- PUT `/api/v1/bookings/:id` — update booking — **PortalAuthMiddleware**
- POST `/custom/create` — custom booking — **JWT (authMiddleWare)**
- GET `/api/v1/bookings` — get bookings — **JWT (authMiddleWare)**

## Portal booking endpoints (reference)

These live under `/api/v1/portal` and are listed in [03-Portal.md](03-Portal.md): create, list, draft CRUD, confirm, get single, update, retry, bulk-confirm, remove.

Request/response shapes must match booking-backend so portal and plugin clients work without change.

## Draft flow (implemented)

Bookings can be **saved as draft** until the user is ready to complete them. Users can **retrieve** drafts, **fill in the rest**, and **confirm** to turn a draft into a completed booking.

- **Domain:** `Booking.IsDraft` (bool). Same table; completed = `IsDraft == false`, drafts = `IsDraft == true`.
- **API:**  
  - `POST /api/v1/portal/bookings/draft` — create draft  
  - `GET /api/v1/portal/bookings/draft` — list drafts  
  - `GET /api/v1/portal/bookings/draft/:id` — get one draft  
  - `PATCH /api/v1/portal/bookings/draft/:id` — update draft  
  - `POST /api/v1/portal/bookings/draft/:id/confirm` — confirm draft → completed booking  
- **Portal:** Bookings list has tabs **Completed** | **Drafts**. Create page has **Save as draft**. Draft detail page at `/bookings/draft/[id]` lets you edit and **Confirm & complete booking**.
- **Dashboard stats** count only completed bookings (excluding drafts).
