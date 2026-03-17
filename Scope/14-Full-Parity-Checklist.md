# Scope: Full booking-backend feature parity checklist

Complete list of routes from booking-backend that must be available in the .NET API. Base prefix for all: `/api/v1`. Use this as the single source for “nothing missing” when implementing or reviewing.

## Health — `/api/v1/health_`

| Method | Path | Auth |
|--------|------|------|
| GET | `/` | none |

## Portal — `/api/v1/portal`

| Method | Path | Auth |
|--------|------|------|
| POST | `/login` | public |
| POST | `/register` | public |
| POST | `/requestPasswordReset` | public |
| POST | `/resetPassword` | public |
| POST | `/verify` | public |
| POST | `/update-status` | public |
| POST | `/send-email` | PortalAuthMiddleware |
| POST | `/bookings/downloadExcel` | PortalAuthMiddleware |
| POST | `/bookings/create` | PortalAuthMiddleware |
| GET | `/bookings` | PortalAuthMiddleware |
| POST | `/bookings/draft` | PortalAuthMiddleware |
| PATCH | `/bookings/draft/:id` | PortalAuthMiddleware |
| GET | `/bookings/draft` | PortalAuthMiddleware |
| GET | `/bookings/draft/:id` | PortalAuthMiddleware |
| POST | `/bookings/:id` | PortalAuthMiddleware |
| GET | `/bookings/:id` | PortalAuthMiddleware |
| PUT | `/bookings/:id` | PortalAuthMiddleware |
| POST | `/bookings/retry/:id` | PortalAuthMiddleware |
| POST | `/bookings/draft/bulk-confirm` | PortalAuthMiddleware |
| POST | `/bookings/draft/remove` | PortalAuthMiddleware |
| POST | `/subscription` | PortalAuthMiddleware |
| GET | `/subscription/:id` | PortalAuthMiddleware |
| PATCH | `/subscription` | PortalAuthMiddleware |
| GET | `/pdf/:id` | PortalAuthMiddleware |
| GET | `/pdf/waybill/:id` | PortalAuthMiddleware |
| GET | `/pdf/draft/:id` | PortalAuthMiddleware |
| GET | `/postnord/track/:id` | PortalAuthMiddleware |
| GET | `/opter/track/:id` | PortalAuthMiddleware |
| GET | `/logisystems/track/:id` | PortalAuthMiddleware |
| GET | `/dhlexpress/track/:id` | PortalAuthMiddleware |
| GET | `/postnord/label/:id` | PortalAuthMiddleware |
| GET | `/dhlexpress/label/:id` | PortalAuthMiddleware |
| GET | `/dhlfreight/label/:id` | PortalAuthMiddleware |
| GET | `/matkahuolto/label/:id` | PortalAuthMiddleware |
| GET | `/plugin` | PortalAuthMiddleware |
| POST | `/plugin/download` | PortalAuthMiddleware |
| GET | `/plugin/release-notes/:tag` | PortalAuthMiddleware |
| GET | `/subusers/:id` | PortalAuthMiddleware |
| GET | `/allBookings` | PortalAuthMiddleware |
| GET | `/favourite` | PortalAuthMiddleware |
| GET | `/getfavouritelist` | PortalAuthMiddleware |
| POST | `/customuser` | PortalAuthMiddleware |
| GET | `/customuser/:quickid` | PortalAuthMiddleware |
| PATCH | `/customuser/:quickid` | PortalAuthMiddleware |
| DELETE | `/customuser/:quickid` | PortalAuthMiddleware |
| POST | `/uploadExcel/:companyid` | PortalAuthMiddleware |

## Company — `/api/v1/company`

| Method | Path | Auth |
|--------|------|------|
| POST | `/default-shipper` | PortalAuthMiddleware |
| PUT | `/default-shipper` | PortalAuthMiddleware |
| GET | `/address-book` | PortalAuthMiddleware |
| POST | `/address-book` | PortalAuthMiddleware |
| PATCH | `/address-book` | PortalAuthMiddleware |
| DELETE | `/address-book` | PortalAuthMiddleware |
| GET | `/details/:id` | PortalAuthMiddleware |
| POST | `/configurations` | PortalAuthMiddleware |
| GET | `/agreement-numbers` | PortalAuthMiddleware |
| PATCH | `/agreement-numbers` | PortalAuthMiddleware |

## Booking — `/api/v1/booking`

| Method | Path | Auth |
|--------|------|------|
| POST | `/api/v1/bookings` | PortalAuthMiddleware |
| PUT | `/api/v1/bookings/:id` | PortalAuthMiddleware |
| POST | `/custom/create` | JWT (authMiddleWare) |
| GET | `/api/v1/bookings` | JWT (authMiddleWare) |

## Plugin — `/api/v1/plugin`

| Method | Path | Auth |
|--------|------|------|
| POST | `/login` | public |
| POST | `/refresh-device` | public |
| GET | `/silent-login` | JWT |
| POST | `/bookings` | JWT |
| GET | `/bookings` | JWT |
| GET | `/bookings/:id` | JWT |
| GET | `/custom-mapping` | JWT |
| POST | `/custom-mapping` | JWT |

## Integration — `/api/v1/integration`

| Method | Path | Auth |
|--------|------|------|
| POST | `/auth` | public |
| POST | `/bookings` | RestApiAuthMiddleware (JWT) |
| GET | `/updates/:patchId` | RestApiAuthMiddleware (JWT) |

## Dashboard — `/api/v1/dashboard`

| Method | Path | Auth |
|--------|------|------|
| GET | `/total` | (commented in backend) |
| GET | `/monthlyavg` | (commented in backend) |
| GET | `/dailyavg/:days` | (commented in backend) |
| GET | `/deliveredavg` | (commented in backend) |

## Token — `/api/v1/token`

| Method | Path | Auth |
|--------|------|------|
| GET | `/` | token.middleware (cargohub-header) |

## Pickup point — `/api/v1/pickuppoint`

| Method | Path | Auth |
|--------|------|------|
| GET | `/postnord` | none |
| GET | `/matkahuolto` | none |
| GET | `/schenker` | none |
| GET | `/posti` | none |
| GET | `/pakettipiste` | none |

## Transport — `/api/v1/transport`

| Method | Path | Auth |
|--------|------|------|
| POST | `/` | none |
| PATCH | `/logisystems` | none |
| PATCH | `/kaukokiitostatus` | none |
| PATCH | `/` | authenticateYourediRequest (Youredi JWT) |

## Swagger / OpenAPI

- OpenAPI spec and Swagger UI at `/docs`, `/openapi`, `/swagger` (or equivalent).

---

Implementation must provide the same HTTP methods, path segments, request/response shapes, and auth for each entry so that CargoHub-portal, plugin clients, and integration clients work without change.
