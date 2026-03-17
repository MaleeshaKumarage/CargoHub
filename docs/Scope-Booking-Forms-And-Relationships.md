# Scope: Booking forms, relationships, and business rules

This document is the single source of truth for booking and company relationships, editability rules, edge cases, and UI error handling. It should be updated when rules or error codes change.

---

## 1. Goals and non-goals

- **Goals**: Explicit Booking→Company; only drafts editable; Super Admin sees all bookings but cannot create; cascade delete company; consistent API error codes and UI error messages.
- **Non-goals (v1)**: Multi-company per user; editing completed bookings; company-scoped booking form design (design page removed).

---

## 2. Roles and actors

| Role | Company | Create booking | List bookings |
|------|---------|----------------|----------------|
| **Super Admin** (no company) | No | No (403) | All companies |
| **Admin** | Yes | Yes (if courier set) | Own company |
| **User** | Yes | Yes (if courier set) | Own company |

---

## 3. Editability rules

- **Completed bookings** (`IsDraft = false`): **Never editable**. Allowed: list, get by id, waybill download. No "update completed booking" API.
- **Drafts** (`IsDraft = true`): Editable via PATCH draft and POST confirm.

---

## 4. Remaining edge cases (consolidated)

| # | Edge case | API behaviour | UI / error message |
|---|-----------|----------------|---------------------|
| 1 | **Super Admin, no company** | Create/draft/confirm: **403** `CompanyRequiredForBooking`. List: return all bookings (customerId=null). | Create/draft page: block or redirect with message. List: show all; optional company filter. |
| 2 | **User has no company** (no BusinessId) | Create/draft: **403** `CompanyRequiredForBooking`. | Show: "Your account is not linked to a company. Contact an administrator." + link to More/support. |
| 3 | **Company not found** (e.g. deleted) | **404** `CompanyNotFound`. | "Company not found. Please refresh or contact support." |
| 4 | **Courier not selected** | Create/draft: **400** `CourierRequired` (and optional field-level errors). | Validate before submit; show "Please select a courier." on field. |
| 5 | **Mandatory fields missing** | Create/draft/update draft: **400** `ValidationFailed` with `errors: { fieldKey: "message" }`. | Show validation errors next to fields (use API errors or client-side). |
| 6 | **Completed booking – edit attempted** | No PATCH for completed; only GET. If any future "edit" endpoint is called for non-draft: **403** `CompletedBookingNotEditable`. | Detail page: no Edit button for completed bookings. |
| 7 | **Company deleted** | Cascade: delete bookings then users then company. Any request for deleted company: **404** `CompanyNotFound`. | Already covered by 3. |
| 8 | **Network / server error** | 5xx or no response. | Generic: "Something went wrong. Please try again." (use existing `auth.networkError`-style key). |
| 9 | **Super Admin list all** | List returns all bookings; optionally include `companyId` / `companyName` per item for filter. | List page: for Super Admin, show company column and optional company filter dropdown. |

---

## 5. API error contract

For the UI to show the right i18n message, the API should return a **stable `errorCode`** (and optional `message`) in error responses.

- Use **JSON body** for 4xx/5xx: `{ "errorCode": "CompanyRequiredForBooking", "message": "…" }`.
- **errorCode** values (suggested; keep in sync with portal messages):
  - `CompanyRequiredForBooking` – user has no company; cannot create booking
  - `CompanyNotFound` – company not found or deleted
  - `CourierRequired` – courier/carrier not selected
  - `ValidationFailed` – mandatory or invalid fields (include `errors` object if needed)
  - `CompletedBookingNotEditable` – completed bookings cannot be edited

Existing auth errors already use `errorCode` (e.g. `CompanyIdRequired`, `CompanyNotFound`). Reuse the same pattern for portal booking and company endpoints.

---

## 6. UI updates and error messages

### 6.1 i18n keys (en.json / fi.json)

Keep a dedicated **`errors`** (or **`bookings.errors`**) section so the portal can map `errorCode` → message. Remove keys that were only for form design: `BookingFormDesignRequired`, `FormDesignAdminOnly`, `FormDesignBlockedByDrafts`, `DraftViewOnlyConfigChanged`, `InvalidFormConfig`, `FormConfigConflict` (or leave for backward compatibility if still referenced).

### 6.2 Pages to update

| Page | Updates |
|------|--------|
| **Bookings list** | For Super Admin: fetch all bookings (pass no customerId or special param); show company column and optional company filter. On error: show `errors[errorCode]` or `errors.generic`. |
| **Create booking** | Validate courier selected; on 403 `CompanyRequiredForBooking`: show message and link to More. On 400 `CourierRequired` / `ValidationFailed`: show field errors. Use `errors[*]` from API. |
| **Draft list** | Same as list for Super Admin if applicable. Error banner with i18n. |
| **Booking detail** (completed) | No Edit button. If any edit action is attempted, show `CompletedBookingNotEditable` (should not happen if UI hides Edit). |
| **API client** (`api.ts`) | For portal booking and company endpoints: parse response body for `errorCode` and `message`; throw or return `{ errorCode, message }` so pages can call `t(\`errors.${errorCode}\`)` or fallback to `message` or `errors.generic`. |

---

## 7. Data model and implementation (summary)

- **Booking.CompanyId** (Guid?, FK): set on create; used for "drafts by company" and cascade on company delete.
- **User.CompanyId** (optional): or keep resolving by BusinessId; needed for cascade delete (delete users with CompanyId = X).
- **Company delete**: Delete bookings where CompanyId = X, then users where CompanyId = X (or BusinessId = company.BusinessId), then company.

---

## 8. Related docs

- Plan files in `.cursor/plans/` (or equivalent) for step-by-step implementation order.
