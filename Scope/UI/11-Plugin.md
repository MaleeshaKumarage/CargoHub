# UI: Plugin

## Route

- **Path**: `[locale]/(protected)/plugin`, optionally `[locale]/(protected)/plugin/[tag]`.
- **Group**: Protected.

## Behaviour

- When backend has plugin endpoints: `GET .../api/v1/portal/plugin` (versions), `POST .../api/v1/portal/plugin/download`, `GET .../api/v1/portal/plugin/release-notes/:tag`.
- Display: Plugin versions, download button, release notes for a tag.
- Stub with links or “Coming soon” until API is ready.

## Components

- shadcn Card, Button; copy from i18n.
