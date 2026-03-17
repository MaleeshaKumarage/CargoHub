# Scope: Integration

Integration (REST API) endpoints under `/api/v1/integration`. Auth: **RestApiAuthMiddleware** (JWT) for all except auth. See [02-Accounts-And-Auth.md](02-Accounts-And-Auth.md) for integration auth.

## Public

- POST `/auth` — integration authentication (returns JWT; client uses Bearer token on subsequent requests)

## Protected (JWT Bearer)

- POST `/bookings` — create booking (uses same handler as portal/booking flow; auth identifies customer)
- GET `/updates/:patchId` — get integration booking updates

Request/response shapes and HTTP methods must match booking-backend (restapi.route.ts, restapi.controller) so integration clients work without change.
