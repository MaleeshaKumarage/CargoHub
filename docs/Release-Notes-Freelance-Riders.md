# Release notes: Freelance riders & booking assignment

**Audience:** Super Admins, company Admins, coordinators, and freelance riders  
**Area:** Portal + API (bookings, invites, rider experience)

---

## Summary

CargoHub now supports **freelance riders**: Super Admins can maintain riders and service areas, coordinators can **assign a rider when completing a booking**, riders receive **invite and assignment** flows, and the portal includes a **lightweight rider area** (deliveries, profile) plus an **accept rider invite** route. Assignments use a configurable **acceptance window**; unaccepted work can **lapse** after the deadline.

---

## What’s new

### Super Admin
- Manage freelance riders (invite, status, service areas) from the portal.
- Rider-related APIs and admin actions aligned with existing auth patterns.

### Coordinators / booking flow
- When finalising a booking, you can pick a **freelance rider** where route and company scope match (postal-based matching; debounced search in the booking UI).
- Booking detail shows **assignment deadline** and lapse state where applicable.

### Riders
- **Accept invite** flow for new rider accounts.
- **Rider shell**: deliveries list and detail, profile.
- Assignments must be **accepted within the configured window** or they lapse (background processing).

### Platform / API
- New domain entities and EF migration for freelance riders and booking assignment fields.
- `RiderAssignment` configuration (see below) in appsettings and `.env.example`.

### Reliability & CI
- Portal Vitest coverage scope and CI Node heap / npm cache adjustments to avoid long-running or OOM coverage jobs.

---

## Configuration

Set explicitly in **all environments** you care about (defaults match **10 minutes** if unset in code paths that bind options—production appsettings now include the section):

| Mechanism | Key |
|-----------|-----|
| Environment variable | `RiderAssignment__AcceptanceWindowMinutes` |
| `appsettings.json` / `*.Production.json` | `RiderAssignment:AcceptanceWindowMinutes` |

See root **`.env.example`** and **`CargoHub.Api/appsettings*.json`** for samples.

---

## Operations / deployment

1. **Apply database migration** on deploy (EF migration for freelance riders + booking columns). API applies migrations on startup if that is your standard.
2. **SMTP / portal base URL**: rider emails depend on existing mail and `Portal` public URL settings—verify like other invite flows.
3. **Roles**: ensure rider role seeding / invites match your environment (first rider still goes through invite).

---

## Plain text (paste into Manage → Release notes)

Use the block below as the **email body** in Super Admin → **Manage → Release notes** (plain text; line breaks are preserved).

```
Subject suggestion: CargoHub update — Freelance riders & booking assignment

CargoHub — Freelance riders & booking assignment

What changed
- Freelance riders: Super Admins can invite and manage riders and their service areas.
- Bookings: when completing a booking, coordinators can assign a matching freelance rider (route/company scoped). Riders must accept within the configured acceptance window.
- Rider portal: riders can sign in to a simple area for deliveries and profile, and use the rider invite acceptance page.
- Configuration: RiderAssignment:AcceptanceWindowMinutes (or RiderAssignment__AcceptanceWindowMinutes in .env) controls how long a rider has to accept; default is 10 minutes unless you override it.

What you should do
- Admins: review Manage → freelance riders (or equivalent) and invite riders as needed.
- Coordinators: use the rider picker on completed bookings where you want a freelance rider; ensure shipper/receiver postals are filled so matching works.
- Operations: deploy the new build and allow the database migration to run; confirm SMTP and portal public URL for emails.

Support
- If assignment emails do not arrive, check SMTP and Portal public base URL settings like other invite flows.
```

---

## Internal references

- PR / merge: freelance rider feature branch merged to `development` (e.g. PR #328 and follow-ups).
- Config parity: `RiderAssignment` documented in `.env.example`, `appsettings.json`, and `appsettings.Production.json`.
