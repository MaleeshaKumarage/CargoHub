# Scope: Company branding and deployment configuration

The application is **de-branded** and **configurable per deployment**: each deployment can present a company name, logo, and theme colors so the product can be white-labelled for any customer.

## Principles

- **No hardcoded product name** in user-facing surfaces (UI copy, PDFs, emails). All such text is driven from configuration or the branding API.
- **Single-tenant per deployment**: One deployment = one company’s branding. Branding is read from appsettings (and optional environment overrides) and exposed to the portal via a public API.
- **Same codebase, different config**: Deploy the same build to multiple environments; each environment sets its own `Branding` section (and optionally DB, CORS, JWT, etc.).

## Branding configuration

Configure in **`CargoHub.Api/appsettings.json`** (or `appsettings.Development.json`, user-secrets, or environment variables) under the **`Branding`** section:

| Key | Description | Example |
|-----|-------------|---------|
| `AppName` | Display name for the portal (navbar, document title). | `"Acme Portal"` |
| `LogoUrl` | URL or path to the company logo (e.g. `/assets/logo.svg` or full URL). | `"/assets/logo.svg"` |
| `PrimaryColor` | Primary theme color (hex). Applied to CSS variables (e.g. `--primary`). | `"#1a1a2e"` |
| `SecondaryColor` | Secondary/accent color (hex). | `"#16213e"` |
| `WaybillFooterText` | Footer text on generated waybill PDFs. | `"Acme — Waybill"` |

Defaults (when omitted or empty): app name fallback `"Portal"`, no logo, no custom colors, waybill footer `"Waybill"` or `"{AppName} — Waybill"`.

## API

- **GET /api/v1/portal/branding** (no auth)  
  Returns JSON: `appName`, `logoUrl`, `primaryColor`, `secondaryColor`.  
  Used by the portal to show the configured name, logo, and theme colors.

## Portal behavior

- On load, the portal calls **GET /api/v1/portal/branding** and stores the result in **BrandingContext**.
- **Navbar**: Shows `appName` (with fallback to a generic i18n key). If `logoUrl` is set, shows the logo image next to or before the name.
- **Theme**: If `primaryColor` / `secondaryColor` are set, the portal applies them to CSS variables (`--primary`, `--secondary`, `--sidebar-primary`) so existing components (buttons, links, sidebar) use the configured colors.
- **Document title and meta description**: Set from branding when available (e.g. `document.title = appName`, meta description `"{AppName} — Booking portal"`).
- **i18n**: Message files provide generic fallbacks (e.g. `"Portal"`, `"Booking portal"`); the branding API overrides the app name in the UI.

## Backend usage of branding

- **Waybill PDF**: `WaybillPdfGenerator` uses `BrandingOptions.WaybillFooterText` or `AppName` for the footer text (no hardcoded product name).
- **Email**: SMTP `FromAddress` is configured in the `Smtp` section (no default product-specific address). If display name or product name is added to emails in future, it should come from `BrandingOptions.AppName`.

## Database and JWT defaults

- Default database name in connection strings and Docker is **`portal`** (see [00-Local-PostgreSQL-Setup.md](00-Local-PostgreSQL-Setup.md)).
- Default JWT issuer/audience in config are generic (e.g. `PortalIssuer`, `PortalAudience`); override via `Jwt:Issuer` and `Jwt:Audience` for production.

## Out of scope (for later)

- **Multi-tenant branding**: Different name/logo/colors per company in a single deployment (would require tenant resolution and a Tenant/Branding table).
- **Admin UI to edit branding**: Currently configuration is file/env only; an admin screen could be added later.
- **Full solution/project rename**: Project/namespace names (e.g. CargoHub.*) are unchanged; only user-facing and configurable strings were de-branded.
