# Scope: Company and user registration

## Company model

- **One company can have multiple user accounts.** Users are linked to a company via the company’s government business ID.
- **Companies are created by Super Admin (or similar role)**, not by self-registration. Use the existing company API (e.g. `POST /api/v1/company`) with appropriate admin-only authorization when that role is implemented.
- **Company identifiers:**
  - **`Id`** (Guid): Internal unique key for listing and DB. Use this for APIs that refer to a company by id.
  - **`BusinessId`** (string): Government business ID (e.g. Finnish Y-tunnus). This is what users enter when registering. Must be set when creating a company and must be unique in practice so registration can look up the company.
  - **`CompanyId`** (string): Logical identifier used across the system; can match BusinessId or be a separate slug.

## User registration

- When **registering**, the user must enter the **Company ID (government business ID)**. Registration is only allowed if a company with that `BusinessId` already exists.
- Flow:
  1. Super admin (or admin) creates a company with the government ID (`BusinessId`) set.
  2. User goes to the portal register page and enters email, user name, password, and **Company ID** (same government ID).
  3. Backend looks up the company by `BusinessId`. If not found, registration fails with a clear error (e.g. “Company must be created by an administrator first”).
  4. If found, the user account is created and linked to that company (e.g. `ApplicationUser.BusinessId` set to the company’s `BusinessId`).

## Portal

- **First page after login is the Dashboard** (not “Home”). Redirect authenticated users to `/dashboard`.
- Registration form includes a required **Company ID** field (government business ID). Show an error if the company does not exist.

## API

- **POST /api/v1/portal/register**  
  Body must include `businessId` (company government ID). If missing or if no company exists with that `BusinessId`, return 400 with `errorCode` and `message` (e.g. `CompanyIdRequired`, `CompanyNotFound`).

- **Company creation**  
  When creating a company (admin API), `BusinessId` (government ID) should be required so that users can later register with it.
