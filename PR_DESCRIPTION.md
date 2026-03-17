## Summary
Adds four distinct UI design themes (Skeuomorphism, Neobrutalism, Claymorphism, Minimalism) that are stored per user in the database and applied on login. Each theme has its own distinctive widgets, button styles, and visual identity.

---

## Backend Changes

- Added `Theme` property to `ApplicationUser` (nullable string)
- Added EF migration `AddUserThemePreference` for Theme column in AspNetUsers
- Extended `GET /api/v1/portal/me` to return `theme` from user profile
- Added `PATCH /api/v1/portal/me/preferences` endpoint to save theme (body: `{ theme }`)
- Theme validation restricted to: skeuomorphism, neobrutalism, claymorphism, minimalism

---

## Frontend Changes (Portal)

- Added `DesignThemeContext` to fetch theme from `/me` on login and save via PATCH on change
- Replaced light/dark theme dropdown with design theme dropdown (4 options) in Navbar
- Added theme selector with Palette icon in Navbar
- Theme-specific CSS for navbar, cards, buttons, inputs, dropdowns, progress bars
- Added `designTheme` i18n translations for all locales (en, fi, sv, no, da, is)
- Extended `getMe` API to include `theme` in response
- Added `updateTheme` API function for PATCH preferences
- Set default `data-theme="minimalism"` on `<html>` for initial render

---

## Theme Styles (Distinctive Widgets & Button Styles)

- **Minimalism**: Clean lines, subtle shadows, generous whitespace (default)

- **Skeuomorphism**: Realistic textures, heavy bevels, physical-material feel
  - Raised navbar with inset highlights
  - Cards/dropdowns with double shadows (outer + inner bevel)
  - Buttons with pressed/raised 3D effect
  - Inputs with recessed inset look

- **Neobrutalism**: Bold, raw, saturated colors
  - Thick 3–4px borders, offset shadows
  - Saturated primary/accent palette (light mode)
  - Bold font weights on buttons/inputs
  - Flat progress bar with border

- **Claymorphism**: Colorful, soft 3D, pill-shaped, playful
  - Pastel palette (soft blues, purples, mint accents)
  - Strong double shadows for 3D depth
  - Pill-shaped buttons (border-radius: 9999px)
  - Rounded navbar (2rem bottom radius)
  - 3D progress bar with inset shadows

---

## Testing

- Backend: 15 tests passed
- Portal: 56 tests passed (including new tests for `getMe` theme, `updateTheme`, `DESIGN_THEMES`)
