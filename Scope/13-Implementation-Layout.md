# Scope: Implementation Layout

High-level files and structure. Follow Clean Architecture + CQRS + MediatR; see [01-Architecture.md](01-Architecture.md) and [02-Accounts-And-Auth.md](02-Accounts-And-Auth.md).

## Application — Accounts module

**Folder:** `CargoHub.Application/Accounts/`

- **Commands**: PortalLoginCommand, PortalRegisterCommand, RequestPasswordResetCommand, ResetPasswordCommand, VerifyEmailCommand, UpdateVerificationStatusCommand, PluginLoginCommand, IntegrationAuthCommand; optional LinkSocialLoginCommand.
- **Handlers**: One per command (e.g. PortalLoginCommandHandler), each implementing IRequestHandler<TReq, TRes>.
- **Dtos**: PortalLoginRequest, PortalLoginResponse (status, data), PortalRegisterRequest/Response, plugin/integration DTOs matching booking-backend.
- **Abstractions**: IPortalCookieService, IWebSocketTokenService (implemented in Infrastructure).
- Optionally merge or reference existing `CargoHub.Application/Auth/` to avoid duplication.

## Application — Other modules (future)

- **Company**: Commands/Queries/Handlers under Application/Company/.
- **Bookings**, **Portal** (bookings, PDF, tracking, labels, etc.): Commands/Queries/Handlers under Application/Bookings/, Application/Portal/, or by feature.

## Infrastructure

- **Identity**: Keep ApplicationUser, Identity config.
- **New — Accounts**: PortalCookieService (implements IPortalCookieService, AES-256-CBC for X-Portal-Cookie). WebSocketTokenService (implements IWebSocketTokenService). Optional email sender for password reset.
- **New**: Portal auth middleware or policy (cookie + customer-id validation) using IPortalCookieService; register in Api pipeline.

## Api — Controllers

- **PortalController** — Route `api/v1/portal`. Actions send MediatR commands (e.g. PortalLoginCommand), set X-Portal-Cookie from result, return DTO (e.g. `{ status: "OK", data: ... }`).
- **PluginController** — Route `api/v1/plugin`. Login sends PluginLoginCommand; protected actions send other commands/queries.
- **IntegrationController** — Route `api/v1/integration`. Auth sends IntegrationAuthCommand; protected actions send commands/queries.
- **CompanyController** — Refactor to use MediatR (CreateCompanyCommand, GetCompanyByIdQuery, etc.); keep route and action paths.
- **Booking** routes under api/v1/portal and/or api/v1/booking; controllers send MediatR commands/queries.

## Configuration

- **Program.cs**: Register IPortalCookieService, IWebSocketTokenService; cookie auth scheme if needed; CORS (portal origin, credentials, customer-id, X-Portal-Cookie); OAuth (Google, Facebook); crypto config (CRYPTO_KEY, CRYPTO_IV).
- **ApplicationUser** and migrations: Ensure CustomerMappingId, verification status, sub-user/role fields as required by portal.

## Phasing

- **Phase 1**: Health, portal auth (login, register, password reset, verify, update-status), cookie + customer-id middleware, plugin/integration auth (JWT), social login.
- **Phase 2**: Every portal protected route and every company route from [14-Full-Parity-Checklist.md](14-Full-Parity-Checklist.md); booking route group.
- **Phase 3**: Plugin, integration, dashboard, token, pickuppoint, transport; Swagger/OpenAPI.
