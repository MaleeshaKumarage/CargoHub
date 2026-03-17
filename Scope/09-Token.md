# Scope: Token

Internal token endpoint under `/api/v1/token`. Auth: **token.middleware** — expects header `hiava-header: hiava-backend` (server-to-server or internal use).

## Endpoints

- GET `/` — create token (handleCreateToken from Youredi controller in booking-backend; returns token for downstream use)

Request/response shape must match booking-backend (token.route.ts) so any internal or legacy callers continue to work.
