# Scope: Transport (Youredi)

Transport/Youredi endpoints under `/api/v1/transport`. Mixed auth: some routes public, one route requires **authenticateYourediRequest** (Youredi JWT in header + body validation).

## Public

- POST `/` — create token (handleCreateToken)
- PATCH `/logisystems` — Logisystems status update
- PATCH `/kaukokiitostatus` — Kaukokiito status update

## Protected (Youredi JWT)

- PATCH `/` — booking update (authenticateYourediRequest: validates Youredi JWT in header and body; uses app keys e.g. private/public key for Youredi)

Request/response shapes and validation must match booking-backend (youredi.route.ts, youredi.controller, youredi.middleware) so transport/carrier integrations continue to work.
