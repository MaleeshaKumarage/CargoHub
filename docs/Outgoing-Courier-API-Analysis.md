# Outgoing Courier API – Analysis & Design

## 1. How booking-backend implements outgoing calls

The reference project (`C:\Users\malee\source\repos\booking-backend`) handles **outgoing** courier integration in three ways, without a single shared interface:

### 1.1 REST/JSON APIs

| Courier        | Service file                    | Operations                          | Config (.env) |
|----------------|----------------------------------|-------------------------------------|----------------|
| DHL Express    | `dhl/dhlexpress-service.ts`      | Create booking, Get tracking       | (Basic auth in code) |
| DHL Freight    | `dhl/dhlfreight-service.ts`      | Token, Create booking, Get label    | - |
| Kaukokiito     | `kaukokiito/kaukokiito-service.ts` | Create transport order (JSON POST) | x-api-key in code |
| Postnord       | `postnord/postnord-service.ts`   | Booking, Pickup, Tracking, Get label| apikey in URL |
| GLS            | `gls/gls-service.ts`             | Post booking                        | - |
| LogiSystems    | `logisystems/logisystems-service.ts` | Token + booking                  | - |
| MyXline        | (referenced in .env)             | Create delivery, Get delivery info   | MYXLINE_*_URL, MYXLINE_API_KEY_* |

All use HTTP client `got` with JSON request/response.

### 1.2 XML over HTTP

| Courier     | Service file                      | Flow                          |
|------------|------------------------------------|-------------------------------|
| Matkahuolto| `matkahuolto/matkahuolto-service.ts` | POST XML body, get XML reply; parse with `xml2js` |

- URL: `https://extservicestest.matkahuolto.fi/mpaketti/mhshipmentxml`
- Content-Type: `text/xml`
- Response: XML with shipment number and label PDF.

### 1.3 Email-based “integration”

| Courier             | Service file                          | Role |
|----------------------|----------------------------------------|------|
| Hämeen Tavarataxi    | `mailServices/hämeenTavarataxiMailService.ts` | Send booking details by email to carrier |
| Scanlink             | `mailServices/scanlinkMailServices.ts` | Same pattern |
| Peltomaa / Sesoma    | `mailServices/peltomaaMailService.ts`, `sesomaMailServices.ts` | Notify by email |

They use a shared `sendEmail` (SMTP) with different templates and recipients. Config: `SMTP_*`, `HAMEEN_TAVARA_TAXI_EMAIL`, etc. in `.env`.

### 1.4 Orchestration (no common interface)

- **Entry:** `db/booking.ts` → `handleExternalProcesses(body, newBooking, ...)`.
- **Flow:** One big try-block that calls, in sequence:
  - `handleCompanyData`, `handleLogitriDivision`, `handleTavoDivision`
  - `handlePostNordTrackingUrl`, `handleLogiApps`, `handleEmail`, `handleLogiSystems`, `handleTrackingNumber`
  - `handleYourEDI`, `handleOpter`
  - **Courier-specific:** `handleDHLExpress`, `handleDHLFreight`, `handleKaukokiito`, `handleMatkahuolto`
- **Selection:** Each handler checks `reqBody.header.postalService === "DHLExpress"` (etc.) and returns early if not applicable. So there is **no** shared “courier client” interface; only a list of if-checks and ad-hoc service calls.
- **Response handling:** Each handler updates the booking (e.g. `carrierId`, `base64_pdf`) via `bookingModel.findByIdAndUpdate`.

So today:

- **Separate** API clients per courier (different files).
- **Mixed** transport: REST, XML, email.
- **No** single interface; adding a courier = new handler + new branch in `handleExternalProcesses` and in the PDF/response logic in `booking.controller.ts`.

---

## 2. Requirements for CargoHub.Backend

- **Separate API (or “client”) per courier** – one class/module per carrier.
- **Multiple transport types:** REST APIs, XML over HTTP, email; all must be pluggable.
- **Single interface** – same contract for create, get status, and related operations so the app code does not branch on transport type.
- **Extensible** – new couriers (API, XML, or email) added by implementing the interface and registering, without changing orchestration.
- **Operations to support:**
  - **Booking creation** (submit to carrier).
  - **Get status / tracking** (where the carrier supports it).
  - Optional: get label, cancel, etc., as needed later.

---

## 3. Proposed design in CargoHub.Backend

### 3.1 Single interface (Application layer)

All courier integrations implement the same interface, regardless of transport:

```csharp
// Application layer
public interface ICourierBookingClient
{
    string CourierId { get; }  // e.g. "DHLExpress", "Matkahuolto", "HämeenTavarataxi"

    Task<CourierCreateResult> CreateBookingAsync(
        CourierCreateRequest request,
        CancellationToken cancellationToken = default);

    Task<CourierStatusResult?> GetStatusAsync(
        string carrierShipmentIdOrReference,
        CancellationToken cancellationToken = default);
}
```

- **CreateBookingAsync:** input is a normalized request (parties, packages, shipment info); each client maps this to REST body, XML, or email content.
- **GetStatusAsync:** input is carrier’s ID or our reference; returns a unified status/tracking result. For email-only couriers this can return `null` or “not supported”.

Shared DTOs (in Application):

- `CourierCreateRequest` – normalized booking data (shipper, receiver, packages, dates, service, test flag, etc.).
- `CourierCreateResult` – success/failure, carrier ID, tracking number, label bytes or base64, optional message.
- `CourierStatusResult` – status code, description, events, tracking URL if any.

### 3.2 One client per courier (Infrastructure)

- **REST:** e.g. `DhlExpressCourierClient`, `KaukokiitoCourierClient`, `PostnordCourierClient`, `MyXlineCourierClient` – use `HttpClient` (or `IHttpClientFactory`) and JSON.
- **XML:** e.g. `MatkahuoltoCourierClient` – use `HttpClient` with `Content-Type: text/xml`, build request with `XmlSerializer` or similar, parse response XML.
- **Email:** e.g. `HameenTavarataxiCourierClient`, `ScanlinkCourierClient` – use `IEmailSender` (or similar) to send a formatted email; `CreateBookingAsync` sends the mail and returns a result with no carrier ID; `GetStatusAsync` returns `null` or “not supported”.

All of them implement `ICourierBookingClient` so the rest of the app only depends on the interface.

### 3.3 Resolution / factory

- **Option A – Factory:** `ICourierBookingClientFactory GetClient(string courierId)` returns the right client for `"DHLExpress"`, `"Matkahuolto"`, `"HämeenTavarataxi"`, etc.
- **Option B – Registry:** Register all clients in DI keyed by courier id; resolve with `IEnumerable<ICourierBookingClient>` or `IDictionary<string, ICourierBookingClient>`.

Orchestration (e.g. after saving a booking) then:

1. Reads `Header.PostalService` (or equivalent) to get `courierId`.
2. Resolves `ICourierBookingClient` for that `courierId`.
3. Builds `CourierCreateRequest` from the booking entity.
4. Calls `CreateBookingAsync`; updates booking with `CourierCreateResult` (carrier ID, label, etc.).
5. For status/tracking, calls `GetStatusAsync` when needed (e.g. from a job or API).

No if-chains on courier name; new couriers = new client + registration.

### 3.4 Configuration

- Keep URLs, API keys, and SMTP settings out of code. Use `IOptions<DhlExpressOptions>`, `IOptions<MatkahuoltoOptions>`, `IOptions<SmtpOptions>`, etc., bound from `appsettings.json` or environment (align with booking-backend `.env`: MYXLINE_*, SFTP_*, SMTP_*, etc.).
- Each client receives only the options it needs (and `HttpClient` or `IEmailSender` where applicable).

---

## 4. Mapping from booking-backend to CargoHub

| booking-backend concept        | CargoHub.Backend                          |
|--------------------------------|-------------------------------------------|
| `postalService` (PostalService enum) | `CourierId` / `Header.PostalService`     |
| `handleDHLExpress` etc.        | `DhlExpressCourierClient.CreateBookingAsync` |
| `handleMatkahuolto`            | `MatkahuoltoCourierClient.CreateBookingAsync` (XML) |
| `handlHämeenTavarataxiEMailServices` | `HameenTavarataxiCourierClient.CreateBookingAsync` (email) |
| `dhlExpressBooking`, `matkahuoltoBooking`, etc. | Same clients, internal HTTP/XML/email |
| `postnordShipmentTracking`, `dhlExpresShipmentTracking` | `GetStatusAsync` on respective client |
| `.env` (MYXLINE_*, SMTP_*, etc.) | Configuration (appsettings / env vars)   |

---

## 5. Summary

- **Current (booking-backend):** Separate services per courier, mixed REST/XML/email, no shared interface; orchestration uses long if-chains on `postalService`.
- **Target (CargoHub.Backend):** One interface `ICourierBookingClient` (create + get status), one implementation per courier (API, XML, or email), resolved by courier id; configuration externalised; easy to extend with new couriers without touching orchestration.

The next step is to add `ICourierBookingClient`, the DTOs, and a factory in the solution, then implement at least one client of each type (REST, XML, email) as a template for the rest.

---

## 6. Implementation summary (CargoHub.Backend)

- **Application:** `ICourierBookingClient`, `ICourierBookingClientFactory`, `CourierCreateRequest` / `CourierCreateResult` / `CourierStatusResult`, `IEmailSender`.
- **Infrastructure:** `CourierBookingClientFactory`, `DhlExpressCourierClient` (REST), `MatkahuoltoCourierClient` (XML), `HameenTavarataxiCourierClient` (email), `SmtpEmailSender`, `ServiceCollectionExtensions.AddCourierClients(configuration)`.
- **Registration in Program.cs:** `builder.Services.AddCourierClients(builder.Configuration);`
- **Config (appsettings or env):** See section 7.

---

## 7. Example configuration (align with booking-backend .env)

```json
{
  "Courier": {
    "DHLExpress": {
      "BaseUrl": "https://express.api.dhl.com/mydhlapi",
      "UseTest": true,
      "TestCreateUrl": "https://express.api.dhl.com/mydhlapi/test/shipments",
      "TestTrackingUrl": "https://express.api.dhl.com/mydhlapi/test/shipments/{id}/tracking",
      "BasicAuthBase64": "<base64 of username:password>"
    },
    "Matkahuolto": {
      "BookingUrl": "https://extservicestest.matkahuolto.fi/mpaketti/mhshipmentxml"
    },
    "HameenTavarataxi": {
      "CarrierEmail": "carrier@example.com",
      "TestEmail": "test@example.com"
    }
  },
  "Smtp": {
    "Host": "smtp.example.com",
    "Port": 465,
    "UseSsl": true,
    "UserName": "noreply@example.com",
    "Password": "<from env INFO_EMAIL_PASSWORD>",
    "FromAddress": "noreply@example.com"
  }
}
```

Environment variables from booking-backend that map here: `SMTP_SERVER_EMAIL`, `SMTP_PORT_EMAIL`, `INFO_EMAIL_ADDRESS`, `INFO_EMAIL_PASSWORD`, `HAMEEN_TAVARA_TAXI_EMAIL`, and any DHL/Matkahuolto URLs and keys you move to config.
