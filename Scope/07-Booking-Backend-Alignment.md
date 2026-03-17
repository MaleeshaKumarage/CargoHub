# Booking-backend alignment

Analysis of **C:\Users\malee\source\repos\booking-backend** and how to align CargoHub.Backend (and portal) with it.

---

## 1. Booking-backend: Booking object

### Structure (completed bookings → `bookings` collection)

| Area | Fields |
|------|--------|
| **Header** | `postalService`, `senderId`, `documentDateTime`, `cargohubId`, `carrierId`, `labelPageLayout`, `companyId`, `senderNumber`, `divisionCode`; some carriers add `serviceType`. |
| **Shipment** | `shipmentNumber`, `shipmentDateTime`, `service`, `dangerousGoods`, `freightPayer`, `senderReference`, `receiverReference`, `extraService`, `incoterm`, `reference`, `pickUpDates`, `deliveryDates`, `handlingInstructions`, `waybillNumber`, `notes` (+ carrier-specific). |
| **Parties** | **Shipper**, **Receiver**, **Payer**, **PickUpAddress**, **DeliveryPoint**. Each: `name`, `address1`, `address2`, `postalCode`, `country`, `city`, `email`, `phoneNumber`, `phoneNumberMobile`, `contactPersonName`, `vatNo`, `taxBorderNumber`, `customerNumber`, `preferredLanguage`. |
| **Package / shipping** | **ShippingInfo**: `grossWeight`, `grossVolume`, `packageQuantity`, pickup/delivery/general instructions, `noDgPackages`, `deliveryWithoutSignature`, `loadmeter`, `routeInformation`, **`packages`** (array: weight, volume, colliCode, packageType, dimensions, description, trackingId, trackingUpdates). |
| **Transport** | **`transportCompany`**: `{ name, data? }` – carrier-specific payload. |
| **Top-level** | `customerId`, `pluginId`, `enabled`, `patchId`, `externalId`, `customerName`, `isSendEmail`, `isTestBooking`, `isFavourite`, `base64_pdf`, `updates` (tracking), `eta`, `createdAt`, `updatedAt`. |

### Draft vs completed

- **No status field** on the document. Distinction is:
  - **Drafts**: stored in **`draftbookings`**; listed with **`enabled: false`**.
  - **Completed**: stored in **`bookings`**; listed with **`enabled: true`**.
- Draft shape is the same as booking; all sub-documents optional (empty `{}`).

### Courier (carrier) in booking-backend

- **Courier = carrier = PostalService.** In that repo it is a **code-defined enum** (Posti, SchenkerLand, SchenkerParcel, Kaukokiito, Matkahuolto, DHLExpress, GLS, etc.) – **not** an admin-managed list.
- **Usage**: `header.postalService` selects carrier; `postalServiceModelMapping` maps to carrier-specific booking models.
- **No CRUD API for couriers** in booking-backend; the “dropdown” is the fixed enum.

**Your requirement:** “adding a courier can be done by admin or super admin” → in CargoHub we should introduce an **admin-managed Courier list** (new entity + API) and use it as the dropdown source, instead of a fixed enum.

---

## 2. CargoHub.Backend today

### Domain (`Booking.cs`)

- **Booking**: Id, CustomerId, PluginId, ExternalId, Enabled, ShipmentNumber, WaybillNumber, CustomerName, IsTestBooking, IsFavourite, Base64Pdf, EtaSeconds, CreatedAtUtc, UpdatedAtUtc.
- **Embedded**: Header (SenderId, CompanyId, ReferenceNumber, **PostalService** string), Shipment (Service, CarrierId, ShipmentDateTimeUtc, ParcelCount), Shipper, Receiver, Payer, PickUpAddress, DeliveryPoint, ShippingInfo (no `packages` array), Updates.
- **BookingParty**: Name, Address1, Address2, PostalCode, City, Country, Email, PhoneNumber, ContactPersonName.
- **ShippingInfo**: GrossWeight/Volume, PackageQuantity, instructions, NoDgPackages, DeliveryWithoutSignature, LoadMeter, RouteInformation – **no per-package array**.

### Application

- **List**: `BookingListDto` – Id, ShipmentNumber, CustomerName, CreatedAtUtc, Enabled, IsFavourite.
- **Detail**: `BookingDetailDto` – full header + Shipper, Receiver, PickUpAddress, DeliveryPoint (no Payer, no Shipment details, no ShippingInfo/packages).
- **Create**: `CreateBookingRequest` – reference + receiver only (no sender, no courier, no package info).
- **Repository**: ListByCustomerId, GetById, Add, GetDashboardStatsAsync. **No draft**; single table/collection.

### Gaps vs booking-backend

| Area | Gap |
|------|-----|
| **Draft vs completed** | No draft entity or collection; no “list drafts”, “create draft”, “confirm draft”, “delete draft”. |
| **Courier** | `PostalService` is free text; no Courier entity or admin CRUD; no dropdown source. |
| **Booking columns** | Missing e.g. senderReference, receiverReference, freightPayer, handlingInstructions, notes, documentDateTime, labelPageLayout, isSendEmail; Shipment has fewer fields. |
| **Parties** | Missing phoneNumberMobile, vatNo, taxBorderNumber, customerNumber, preferredLanguage. |
| **Package info** | No **packages** array (per-package weight, volume, type, dimensions, etc.); ShippingInfo has no packages. |
| **Create request** | Minimal (reference + receiver); no sender, courier, payer, pickup/delivery, package(s). |
| **List/Detail DTOs** | List: no courier/postalService; Detail: no Payer, no Shipment details, no ShippingInfo/packages. |

---

## 3. Recommended alignment (CargoHub)

### 3.1 Draft vs completed

- Add **DraftBooking** (or store drafts in same table with a **Status** or **IsDraft** flag).
  - Option A: Separate table `DraftBookings` (like booking-backend’s `draftbookings`).
  - Option B: Same table `Bookings` with **Status** = `Draft` | `Completed` (or **IsDraft** bool); list “drafts” and “completed” by filter.
- **API**:
  - `POST /api/v1/portal/bookings/draft` – create draft.
  - `GET /api/v1/portal/bookings/draft` – list drafts (paginated).
  - `GET /api/v1/portal/bookings/draft/:id` – get draft.
  - `PATCH /api/v1/portal/bookings/draft/:id` – update draft.
  - `POST /api/v1/portal/bookings/draft/:id/confirm` – confirm draft → create completed booking, (optionally) delete draft.
  - Keep `GET /api/v1/portal/bookings` for **completed** only.

### 3.2 Courier as dropdown (admin-managed)

- Add **Courier** entity: Id, Name, Code (e.g. Posti, DHL), IsActive, sort order, company-scoped or global.
- **Admin/Super Admin**: CRUD for couriers (list, add, edit, activate/deactivate).
- **Portal**: GET list of active couriers (e.g. `GET /api/v1/portal/couriers` or `/api/v1/admin/couriers`) for dropdown.
- **Booking**: Store **CourierId** (FK) and/or **PostalService** (code/name); create/list/detail and dashboard stats by courier use this.

### 3.3 Booking object and DTOs

- **Domain**: Add missing header/shipment fields as needed; add **Packages** (collection of package items: weight, volume, type, dimensions, etc.); extend **BookingParty** with phoneNumberMobile, vatNo, taxBorderNumber, customerNumber, preferredLanguage.
- **CreateBookingRequest**: Extend with sender, receiver, payer, pickup/delivery, **courier (CourierId or code)**, and **packages** (array).
- **BookingListDto**: Add **PostalService/CourierName** (for display).
- **BookingDetailDto**: Add Payer, full Shipment, ShippingInfo and **packages** array.

### 3.4 Portal UI

- **Bookings list**: Tabs or filter for “Drafts” vs “Completed”; show courier in list.
- **Create booking**: Full form with sender, receiver, payer, pickup/delivery, **courier dropdown**, package(s); optional “Save as draft”.
- **Draft detail**: Edit and “Confirm” to create completed booking.

---

## 4. Summary

| Topic | booking-backend | CargoHub alignment |
|-------|-----------------|--------------------|
| **Booking object** | Rich: header, shipment, shipper, receiver, payer, pickup, delivery, shippingInfo + packages, transportCompany, updates | Extend domain + DTOs with missing fields and **packages** array. |
| **Courier** | Enum in code; no admin CRUD | Add **Courier** entity + admin CRUD; portal dropdown from API. |
| **Draft vs completed** | Separate collections + `enabled` | Add drafts (table or status) + draft API + “confirm draft” flow. |
| **Create** | Full document (or draft) | Extend CreateBookingRequest to full sender/receiver/package/courier. |

This document can be used as the scope for implementing draft/completed, courier dropdown (admin-managed), and the richer booking object in CargoHub.Backend and the portal.
