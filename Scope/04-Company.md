# Scope: Company

Company-related endpoints under `/api/v1/company`. Auth: **PortalAuthMiddleware** (cookie + `customer-id`). Implement via CQRS/MediatR (e.g. CreateDefaultShipperCommand, GetAddressBookQuery); see [01-Architecture.md](01-Architecture.md).

## Endpoints

- POST `/default-shipper` — create default shipper address
- PUT `/default-shipper` — update default shipper address
- GET `/address-book` — fetch address book
- POST `/address-book` — add address to address book
- PATCH `/address-book` — update company address book
- DELETE `/address-book` — delete company address book
- GET `/details/:id` — company details
- POST `/configurations` — add configurations
- GET `/agreement-numbers` — fetch agreement numbers (with validateAgreementNumber content validation)
- PATCH `/agreement-numbers` — update agreement numbers

Request/response shapes and HTTP methods must match booking-backend (company.route.ts, company.controller).
