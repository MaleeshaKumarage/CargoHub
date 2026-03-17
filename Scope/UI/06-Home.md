# UI: Home (protected entry)

## Route

- **Path**: `[locale]/(protected)/home`.
- **Group**: Protected (requires valid token; redirect to login if not authenticated).

## Behaviour

- Greeting using current user (displayName or email).
- Shortcuts: Bookings list, Create booking, Actions, Plugin, More (or equivalent).
- Layout: Use protected layout (navbar with theme + locale switcher, user menu, logout).

## Components

- shadcn Card, Button; layout from protected shell.
- Copy from i18n.
