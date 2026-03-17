# Scope: Health and Swagger

## Health

**Base path:** `/api/v1/health_`

- GET `/` — health check (returns success so load balancers and monitors can verify the API is up)

Response shape can be minimal (e.g. 200 OK and optional body); match or simplify booking-backend health route.

## Swagger / OpenAPI

- Expose **OpenAPI spec** (e.g. `/openapi`, `/swagger/v1/swagger.json` or equivalent).
- Expose **Swagger UI** for API documentation at one or more of: `/docs`, `/openapi`, `/swagger` (as in booking-backend).

Enables discovery and testing of all api/v1 endpoints; keep tags/grouping aligned with route areas (portal, company, plugin, integration, etc.) for future use.
