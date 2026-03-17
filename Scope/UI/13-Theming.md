# UI: Theming (light / dark / system)

## Approach

- **next-themes**: Wrap app in `ThemeProvider` with `attribute="class"`, `defaultTheme="system"`, `enableSystem`. Persists choice (e.g. in localStorage).
- **Tailwind**: Use `dark:` variants in CSS; ensure shadcn theme (CSS variables in `app/globals.css` and/or `tailwind.config`) defines both light and dark so all components and custom pages respect theme.
- **Switcher**: Provide control (dropdown or toggle) in navbar or user menu: Light / Dark / System. Use `useTheme()` from next-themes to set `theme` and `setTheme`.

## Branding colors

- When the API returns branding with `primaryColor` / `secondaryColor`, the portal applies them to CSS variables (`--primary`, `--secondary`, `--sidebar-primary`) so the app uses the configured company colors. See [Scope 16-Branding-And-Deployment.md](../16-Branding-And-Deployment.md).

## Requirements

- No flash of wrong theme on load (next-themes handles with `suppressHydrationWarning` on `<html>` if needed).
- All shadcn and custom UI must look correct in light and dark.
