# Scope: Pickup Point

Pickup point endpoints under `/api/v1/pickuppoint`. In booking-backend these have no auth; keep same or add auth if required for .NET.

## Endpoints

- GET `/postnord` — Postnord pickup points
- GET `/matkahuolto` — Matkahuolto pickup points
- GET `/schenker` — Schenker pickup points
- GET `/posti` — Posti pickup points
- GET `/pakettipiste` — Pakettipiste pickup points

Request/response shapes (and any query params for location/filters) must match booking-backend (pickuppoint.route.ts) so portal or other clients work without change.
