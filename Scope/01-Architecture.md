# Scope: Architecture

Clean Architecture, CQRS, MediatR, and separated user-account handling.

## Single PostgreSQL database

**User and application data use the same PostgreSQL database.** One connection string (`DefaultConnection`) and one EF Core context (`ApplicationDbContext`): Identity (user) tables and domain tables (Company, Booking, Courier, etc.) all live in that database. Do not introduce a separate database or DbContext for user data.

## Layers and dependency rule

- **Domain** — Entities, value objects, domain events. No dependencies on Application or Infrastructure.
- **Application** — Use cases only. Commands, Queries, Handlers (MediatR), DTOs, and interfaces (abstractions) for external concerns. Depends only on Domain. No ASP.NET Core or EF in Application (use delegates or interfaces for Identity/cookie/JWT if needed).
- **Infrastructure** — Implementations: EF Core, Identity, JWT, cookie encryption, email, OAuth. Implements interfaces defined in Application (or receives delegates from Api). Depends on Application and Domain.
- **Api** — Controllers, middleware, pipeline. Sends Commands/Queries via `IMediator`; no business logic. Depends on Application and Infrastructure for DI.

## CQRS + MediatR

- Every use case is a **Command** (`IRequest<T>`) or **Query** (`IRequest<T>`). Each has a single **Handler** (`IRequestHandler<TReq, TRes>`).
- Controllers only: `await _mediator.Send(commandOrQuery)` and return the result (or map to HTTP status/body). No direct DbContext or Identity calls in controllers.
- Extend the existing auth pattern (LoginUserCommand, RegisterUserCommand, handlers) to portal login/register, password reset, verify, plugin/integration auth, and to company, bookings, etc. Refactor CompanyController to use MediatR instead of ApplicationDbContext directly.

## User account handling — separated

**Accounts** is a dedicated vertical slice (bounded context) for registration, login, verification, password reset, social linking, and sub-users.

**Option A (recommended):** Single Application assembly, separate by folder/namespace:

- **CargoHub.Application/Accounts/** — All account-related use cases:
  - **Commands**: PortalLoginCommand, PortalRegisterCommand, RequestPasswordResetCommand, ResetPasswordCommand, VerifyEmailCommand, UpdateVerificationStatusCommand, PluginLoginCommand, IntegrationAuthCommand; optional LinkSocialLoginCommand.
  - **Queries**: GetUserByEmailQuery, GetSubUsersQuery, etc., if needed.
  - **Handlers**: One per command/query.
  - **Dtos**: Portal response DTOs (PortalLoginResponse, PortalRegisterResponse, etc.) and request DTOs.
  - **Abstractions**: IPortalCookieService, IWebSocketTokenService (defined in Application, implemented in Infrastructure).
- **Domain**: Add account-related value objects or entities only if not purely Identity; Domain can stay minimal for accounts.
- **Infrastructure**: Keep CargoHub.Infrastructure/Identity/ and add CargoHub.Infrastructure/Accounts/ for cookie encryption, token issuance, and account-specific persistence. Implement Application account abstractions here.

**Option B:** Separate assembly (e.g. CargoHub.Application.Accounts) for a strict physical boundary.

**Api**: Controllers (PortalController, PluginController, IntegrationController) only send MediatR commands/queries from the Accounts module and set cookies/headers from handler results; no account logic in controllers.
