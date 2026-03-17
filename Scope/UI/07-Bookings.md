# UI: Bookings list

## Route

- **Path**: `[locale]/(protected)/bookings`.
- **Group**: Protected.

## Behaviour

- Fetch: `GET {NEXT_PUBLIC_API_URL}/api/v1/portal/bookings` (when backend implements; use pagination/filters per API contract). Send `Authorization: Bearer` and `customer-id` headers.
- Display: Table or list of bookings (e.g. shipment number, date, status). Use shadcn Table or DataTable.
- Actions: Link to booking detail; link to create booking.

## Components

- shadcn Table or DataTable, Button, Skeleton for loading.
- Copy from i18n.
