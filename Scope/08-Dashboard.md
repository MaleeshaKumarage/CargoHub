# Scope: Dashboard

Dashboard endpoints under `/api/v1/dashboard`. In booking-backend, auth middleware is currently commented out; decide whether to require portal or JWT auth in .NET for consistency.

## Endpoints

- GET `/total` — bookings count
- GET `/monthlyavg` — monthly average
- GET `/dailyavg/:days` — daily average (e.g. last N days)
- GET `/deliveredavg` — delivered average

Request/response shapes and query parameters must match booking-backend (dashboard.route.ts, dashboard.controller). Typically expect `customer-id` or similar context for scoping data.
