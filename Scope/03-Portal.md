# Scope: Portal

All portal endpoints under `/api/v1/portal`. Auth: **PortalAuthMiddleware** (cookie `X-Portal-Cookie` + header `customer-id`) for all except the first 6 public routes. See [02-Accounts-And-Auth.md](02-Accounts-And-Auth.md) for auth details.

## Public (no auth)

- POST `/login` — portal login
- POST `/register` — portal register
- POST `/requestPasswordReset` — request password reset
- POST `/resetPassword` — reset password
- POST `/verify` — verify email with code
- POST `/update-status` — update verification status

## Protected (cookie + customer-id)

**Email / Excel**

- POST `/send-email` — generate Excel and send email
- POST `/bookings/downloadExcel` — generate Tavo Excel

**Bookings**

- POST `/bookings/create` — create new booking
- GET `/bookings` — list user bookings (pagination, filters)
- POST `/bookings/draft` — create draft
- PATCH `/bookings/draft/:id` — update draft
- GET `/bookings/draft` — list drafts
- GET `/bookings/draft/:id` — get single draft
- POST `/bookings/:id` — confirm booking
- GET `/bookings/:id` — get single booking
- PUT `/bookings/:id` — update booking
- POST `/bookings/retry/:id` — retry booking
- POST `/bookings/draft/bulk-confirm` — bulk confirm drafts
- POST `/bookings/draft/remove` — remove drafts

**Subscription**

- POST `/subscription` — create subscription (plugin)
- GET `/subscription/:id` — get subscription
- PATCH `/subscription` — update subscription

**PDF**

- GET `/pdf/:id` — get PDF
- GET `/pdf/waybill/:id` — get waybill PDF
- GET `/pdf/draft/:id` — get draft PDF

**Tracking**

- GET `/postnord/track/:id` — Postnord tracking
- GET `/opter/track/:id` — Opter tracking
- GET `/logisystems/track/:id` — Logisystems tracking
- GET `/dhlexpress/track/:id` — DHL Express tracking

**Labels**

- GET `/postnord/label/:id` — Postnord label
- GET `/dhlexpress/label/:id` — DHL Express label
- GET `/dhlfreight/label/:id` — DHL Freight label
- GET `/matkahuolto/label/:id` — Matkahuolto label

**Plugin (portal)**

- GET `/plugin` — get plugin versions
- POST `/plugin/download` — download plugin
- GET `/plugin/release-notes/:tag` — plugin release notes

**Users**

- GET `/subusers/:id` — get sub-users
- GET `/allBookings` — multi-company bookings

**Favourites**

- GET `/favourite` — favourite list
- GET `/getfavouritelist` — get favourite list

**Custom user (Devoca/Tavo)**

- POST `/customuser` — create custom user
- GET `/customuser/:quickid` — get custom user
- PATCH `/customuser/:quickid` — update custom user
- DELETE `/customuser/:quickid` — delete custom user

**Upload**

- POST `/uploadExcel/:companyid` — upload Excel

Implementation must match booking-backend request/response shapes and HTTP methods so CargoHub-portal works without change.
