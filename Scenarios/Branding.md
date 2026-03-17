# Branding (company name, logo, colors)

## Configured branding is shown in the portal

If the API is configured with a company name, logo URL, and theme colors (via the `Branding` section in appsettings), and the user opens the portal, then the portal fetches branding from **GET /api/v1/portal/branding** and shows the configured app name in the navbar and in the browser tab title, the logo (if set) next to the name, and the primary and secondary colors applied to buttons, links, and other themed elements. If the API returns empty or the request fails, the portal falls back to a generic name (e.g. "Portal"), no logo, and default theme colors.

## Waybill PDF uses configured footer

If the user downloads a waybill PDF for a completed booking, then the PDF footer shows the configured waybill footer text (from branding config), or the configured app name plus "— Waybill", or the generic "Waybill" when nothing is configured. No product-specific name appears in the PDF.

## Deployment can be white-labelled

If a deployer sets in configuration a company name, logo path or URL, and hex colors for primary and secondary theme, then that deployment presents the portal and generated documents (e.g. waybill) under that company’s branding, with no reference to a fixed product name. The same codebase can be deployed for different customers with different appsettings (or environment variables).
