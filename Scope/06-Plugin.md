# Scope: Plugin

Plugin endpoints under `/api/v1/plugin`. Auth: **JWT (authMiddleWare)** for all except login and refresh-device. Login returns JWT with claims `iss: "Hiava"`, `plugin-id`, `customer-id`. See [02-Accounts-And-Auth.md](02-Accounts-And-Auth.md) for plugin auth.

## Public

- POST `/login` — plugin login (returns JWT and plugin/customer ids)
- POST `/refresh-device` — refresh dead device

## Protected (JWT Bearer)

- GET `/silent-login` — silent login
- POST `/bookings` — create booking from plugin
- GET `/bookings` — list plugin bookings
- GET `/bookings/:id` — single plugin booking (note: in booking-backend route is `"bookings/:id"` without leading slash; ensure full path matches client usage)
- GET `/custom-mapping` — get custom mapping
- POST `/custom-mapping` — create custom mapping

Request/response shapes and HTTP methods must match booking-backend (plugin.route.ts, plugin.controller) so plugin clients work without change.
