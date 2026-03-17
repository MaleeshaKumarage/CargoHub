# Database entity diagram

PostgreSQL with three schemas: **auth**, **companies**, **bookings**.

---

## Full ER diagram (all connections)

Single diagram showing every entity and relationship. Labels **FK** = database foreign key; **app** = application-level link (no FK in DB).

```mermaid
erDiagram
    AspNetUsers ||--o{ AspNetUserRoles : "roles"
    AspNetRoles ||--o{ AspNetUserRoles : "roles"
    AspNetUsers }o--|| Companies : "BusinessId app"
    Bookings }o--|| AspNetUsers : "CustomerId app"
    Companies ||--o{ Bookings : "CompanyId FK"
    Companies ||--o{ CompanySenders : "senders"
    Companies ||--o{ CompanyReceivers : "receivers"
    Companies ||--o{ CompanyPickUp : "pickup"
    Companies ||--o{ CompanyAgreements : "agreements"
    Bookings ||--o{ BookingStatusHistory : "status"
    Bookings ||--o{ BookingPackage : "packages"
    Bookings ||--o{ BookingUpdate : "updates"
    AspNetUsers {
        string Id PK
        string BusinessId
        string UserName
        string Email
    }
    AspNetRoles {
        string Id PK
        string Name
    }
    Companies {
        guid Id PK
        string CompanyId
        string Name
        string BusinessId
    }
    Bookings {
        guid Id PK
        string CustomerId
        guid CompanyId FK
        bool IsDraft
        datetime CreatedAtUtc
    }
    CompanySenders {
        guid Id PK
        guid CompanyId FK
        string Name
        string Address1
    }
    CompanyReceivers {
        guid Id PK
        guid CompanyId FK
        string Name
    }
    CompanyPickUp {
        guid Id PK
        guid CompanyId FK
    }
    CompanyAgreements {
        guid Id PK
        guid CompanyId FK
        string PostalService
        string Number
    }
    BookingStatusHistory {
        guid Id PK
        guid BookingId FK
        string Status
        datetime OccurredAtUtc
    }
    BookingPackage {
        guid BookingId FK
        int Id PK
        string Weight
    }
    BookingUpdate {
        guid BookingId FK
        int Id PK
        string Status
    }
```

---

## Overview

```mermaid
flowchart LR
    subgraph auth [auth schema]
        Users[AspNetUsers]
        Roles[AspNetRoles]
    end
    subgraph companies [companies schema]
        Companies[Companies]
    end
    subgraph bookings [bookings schema]
        Bookings[Bookings]
        Status[BookingStatusHistory]
        Packages[BookingPackages]
    end
    Users <-->|"BusinessId match\n(no FK)"| Companies
    Users -->|"CustomerId = User.Id\n(no FK)"| Bookings
    Companies -->|"CompanyId FK\n1:N"| Bookings
    Bookings --> Status
    Bookings --> Packages
```

- **Company ↔ Users**: A company is linked to its users by **BusinessId** (user.BusinessId = company.BusinessId). One company has many users; no DB foreign key.
- **Booking → User**: Every booking has **CustomerId** = AspNetUsers.Id (the user who owns the booking). No DB foreign key; enforced in application code.
- **Booking → Company**: **CompanyId** is a real FK to Companies.Id (booking belongs to that company/shipper).

---

## 1. Auth schema

Identity tables: users, roles, and join tables.

```mermaid
erDiagram
    AspNetUsers ||--o{ AspNetUserRoles : ""
    AspNetRoles ||--o{ AspNetUserRoles : ""
    AspNetUsers {
        string Id PK
        string UserName
        string Email
        string DisplayName
        string BusinessId
        string CustomerMappingId
        bool IsActive
    }
    AspNetRoles {
        string Id PK
        string Name
    }
    AspNetUserRoles {
        string UserId FK
        string RoleId FK
    }
```

| Table | Purpose |
|-------|--------|
| **auth.AspNetUsers** | Users (Id, UserName, Email, DisplayName, BusinessId, CustomerMappingId, IsActive) |
| **auth.AspNetRoles** | Roles (e.g. SuperAdmin, Admin, User) |
| **auth.AspNetUserRoles** | User–Role many-to-many |
| auth.AspNetUserClaims, AspNetUserLogins, AspNetUserTokens, AspNetRoleClaims | Identity claims, logins, tokens |

---

## 2. Companies schema

One **Companies** row per tenant/shipper; address books and agreement numbers are owned child data. **Users** belong to a company by matching **BusinessId** (ApplicationUser.BusinessId = Companies.BusinessId); there is no FK—the app resolves “user’s company” by this match.

```mermaid
erDiagram
    Companies ||--o{ Senders : "senders"
    Companies ||--o{ Receivers : "receivers"
    Companies ||--o{ PickUp : "pickup"
    Companies ||--o{ Agreements : "agreements"
    Companies {
        guid Id PK
        string CompanyId
        string Name
        string BusinessId
        string CustomerId
        string SenderNumber
        int Counter
    }
    Senders {
        guid Id PK
        guid CompanyId FK
        string Name
        string Address1
        string City
        string Country
    }
    Receivers {
        guid Id PK
        guid CompanyId FK
        string Name
        string Address1
    }
    PickUp {
        guid Id PK
        guid CompanyId FK
        string Name
    }
    Agreements {
        guid Id PK
        guid CompanyId FK
        string PostalService
        string Number
    }
```

| Table | Purpose |
|-------|--------|
| **companies.Companies** | Company (Id, CompanyId, Name, BusinessId, CustomerId, SenderNumber, DivisionCode, Counter). Optional owned: DefaultShipperAddress, Configurations. |
| Companies_SenderAddressBook | Sender address book (CompanyId FK) |
| Companies_AddressBook | Receiver address book (CompanyId FK) |
| Companies_PickUpAddressBook | Pick-up addresses (CompanyId FK) |
| Companies_AgreementNumbers | Agreement numbers per postal service (CompanyId FK) |

---

## 3. Bookings schema

**Bookings** is the main aggregate; status history, packages, and updates are child tables. Each booking belongs to a **user** (CustomerId) and a **company** (CompanyId FK).

```mermaid
erDiagram
    AspNetUsers ||--o{ Bookings : "CustomerId app"
    Companies ||--o{ Bookings : "CompanyId FK"
    Bookings ||--o{ BookingStatusHistory : "status"
    Bookings ||--o{ BookingPackage : "packages"
    Bookings ||--o{ BookingUpdate : "updates"
    AspNetUsers {
        string Id PK
        string BusinessId
    }
    Bookings {
        guid Id PK
        string CustomerId
        guid CompanyId FK
        string ShipmentNumber
        string WaybillNumber
        bool IsDraft
        bool Enabled
        datetime CreatedAtUtc
    }
    BookingStatusHistory {
        guid Id PK
        guid BookingId FK
        string Status
        datetime OccurredAtUtc
        string Source
    }
    BookingPackage {
        guid BookingId FK
        int Id PK
        string Weight
        string Volume
        string PackageType
    }
    BookingUpdate {
        guid BookingId FK
        int Id PK
        string Status
    }
    Companies {
        guid Id PK
    }
```

| Table | Purpose |
|-------|--------|
| **bookings.Bookings** | Booking (Id, CustomerId, CompanyId FK, ShipmentNumber, WaybillNumber, IsDraft, Enabled, CreatedAtUtc, UpdatedAtUtc). Owned columns: Header, Shipment, Shipper, Receiver, Payer, PickUpAddress, DeliveryPoint, ShippingInfo. |
| **bookings.BookingStatusHistory** | One row per milestone (Draft, Waybill, Confirmed, etc.); BookingId FK. |
| Bookings_Packages | Packages for the booking; BookingId FK, composite PK (BookingId, Id). |
| Bookings_Updates | Transport/tracking updates; BookingId FK, composite PK (BookingId, Id). |

---

## Key relationships

| From | To | Type |
|------|-----|------|
| **AspNetUsers** | **Companies** | Application: user.BusinessId = company.BusinessId → one company has many users (no FK) |
| **Bookings.CustomerId** | **AspNetUsers.Id** | Application: booking belongs to one user (no FK) |
| Bookings.CompanyId | Companies.Id | FK (optional, cascade delete) |
| BookingStatusHistory.BookingId | Bookings.Id | FK |
| BookingPackage.BookingId | Bookings.Id | FK (owned) |
| BookingUpdate.BookingId | Bookings.Id | FK (owned) |
| Company address/agreement tables | Companies.Id (CompanyId) | FK (owned) |

---

## Notes

- **Schemas**: `auth` = Identity; `companies` = company aggregate; `bookings` = booking aggregate.
- **Company has users**: Not stored as a FK. Users with the same BusinessId as a company are considered that company’s users (resolved in code).
- **Booking has user**: Bookings.CustomerId holds AspNetUsers.Id; no FK in the DB. The app uses it to list “my bookings” and enforce ownership.
- **Owned types**: `OwnsOne` (e.g. Header, Configurations) are usually columns on the parent table; `OwnsMany` (Packages, Updates, address books) are separate tables with a FK to the parent.
