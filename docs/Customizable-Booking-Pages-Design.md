# Customizable booking pages (sections, columns, real DB columns)

## Overview

Design company-specific booking form layout (sections and columns, mandatory/optional) with custom field values stored as real columns on the main Bookings table by using a fixed pool of custom columns plus per-company form configuration.

## Constraint: "New columns on main table"

- **Approach A (implemented):** Fixed custom columns on `Bookings` (e.g. `CustomString1`..`CustomString20`, `CustomDecimal1`..`CustomDecimal5`, `CustomDate1`..`CustomDate5`). When a company "adds a column", they claim one slot and give it a label and mandatory/optional.
- **Company-scoped retrieve:** API returns only custom field values for slots defined in that company's form config.
- **Company-scoped insert:** API accepts and persists only custom slot keys that appear in that company's form config.

## Data model

- **Booking:** `BookingCustomFields` owned type with fixed slot properties.
- **Company:** `BookingFormConfig` (sections and fields; built-in keys or custom slots, mandatory/optional), stored as JSON or owned entities.

## API

- `GET /api/v1/portal/company/booking-form-config` — get current company's form config.
- `PUT /api/v1/portal/company/booking-form-config` — save form config (admin/company).
- Create/update booking and draft: request/response include `customFields` dictionary; backend filters by company config on read and write.

## Portal

- **Design page:** `/[locale]/(protected)/manage/booking-form` — configure sections, fields (built-in vs custom slot), mandatory/optional; GET/PUT form config.
- **Create/Draft pages:** Load form config and render form dynamically; submit and display only company-configured custom fields.

See plan file in `.cursor/plans/` for full mermaid flow and details.
