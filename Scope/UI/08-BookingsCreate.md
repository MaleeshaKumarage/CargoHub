# UI: Create booking

## Route

- **Path**: `[locale]/(protected)/bookings/create`.
- **Group**: Protected.

## Behaviour

- Submit: `POST {NEXT_PUBLIC_API_URL}/api/v1/portal/bookings/create` (or draft when available) with booking payload. Headers: Authorization, customer-id.
- Form: Fields per backend contract (shipper, receiver, service, etc.). Start minimal and expand as API is defined.
- Success: Redirect to bookings list or new booking detail.
- Failure: Show validation or API error.

## Components

- shadcn Form, Input, Select; optional stepper for long forms.
- Copy from i18n.
