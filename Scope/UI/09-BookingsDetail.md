# UI: Booking detail

## Route

- **Path**: `[locale]/(protected)/bookings/[id]`.
- **Group**: Protected.

## Behaviour

- Fetch: `GET {NEXT_PUBLIC_API_URL}/api/v1/portal/bookings/:id` with auth headers.
- Display: Read-only or editable view of booking (header, shipment, shipper, receiver, status, PDF link, etc. per API response).
- Actions: Link to PDF, waybill when backend has routes; optional edit/retry when API supports.

## Components

- shadcn Card, Badge, Button; layout consistent with list and create.
- Copy from i18n.
