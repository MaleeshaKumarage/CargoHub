# UI: Setup and scaffold

## Folder and tooling

- **Location**: `portal/` at repo root (sibling to `CargoHub.Api/`, `Scope/`).
- **Scaffold**: Next.js 15 with TypeScript, ESLint, Tailwind, App Router, `src/` directory.
- **Components**: shadcn/ui (init via `npx shadcn@latest init`; use `components.json` for paths).
- **Providers**: next-themes (`ThemeProvider`), next-intl (`NextIntlClientProvider`); both in root layout.

## Environment

- **`.env.local`**: `NEXT_PUBLIC_API_URL=http://localhost:5299` (or the URL of the running .NET API).
- **`.env.local.example`**: Same variable documented for other developers.
- All API calls use `NEXT_PUBLIC_API_URL + path` (e.g. `/api/v1/portal/login`).

## CORS (backend)

- In `CargoHub.Api` `Program.cs`, allow the portal origin (e.g. `http://localhost:3000` for dev), credentials, and required headers (`Authorization`, `customer-id`, `Content-Type`).

## Run

- `cd portal && npm install && npm run dev` — dev server (e.g. http://localhost:3000).
- Build: `npm run build`; production: `npm start` or deploy output to static/SPA host.

## Docs

- `portal/README.md`: How to run, env vars, and link to this Scope/UI folder.
- Update [Scope/15-UI-Portal.md](../15-UI-Portal.md) to point to `Scope/UI/` and `portal/`.
