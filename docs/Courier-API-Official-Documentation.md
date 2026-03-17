# Courier APIs – Official documentation, endpoints & credentials

This document links to **official** documentation and describes how to obtain **valid** endpoints and credentials for each courier. Use it to replace or verify the values used in booking-backend and in CargoHub courier clients.

---

## 1. DHL Express (MyDHL API)

### Official documentation
- **API reference & overview:** [MyDHL API (DHL Express)](https://developer.dhl.com/api-reference/dhl-express-mydhl-api)
- **Find credentials:** [Find your API Credentials](https://developer.dhl.com/getting-started/find-your-api-credentials?language_content_entity=en)
- **Test your integration:** [Test your Integration](https://developer.dhl.com/getting-started/test-your-integration?language_content_entity=en)
- **Get access (existing customer):** [Get Access with blocklist](https://developer.dhl.com/express/get-access-with-blocklist/112)
- **New customer account:** [DHL Express customer account request](https://mydhl.express.dhl/gb/en/ship/open-account.html)

### Base URLs (from official docs)
| Environment | Base URL |
|-------------|----------|
| **Test** | `https://express.api.dhl.com/mydhlapi/test` |
| **Production** | `https://express.api.dhl.com/mydhlapi` |

### Endpoints (append to base URL)
- **Create shipment:** `POST /shipments` (optional query: `?strictValidation=false&bypassPLTError=false&validateDataOnly=false`)
- **Tracking:** `GET /shipments/{shipmentId}/tracking`

### Authentication
- **Method:** HTTP **Basic Authentication**
- **Header:** `Authorization: Basic <Base64(APIKey:APISecret)>`
- **Where to get credentials:**  
  - Your **DHL Express consultant** provides API access credentials for MyDHL API.  
  - You can also see **API Key** and **API Secret** in your [Developer Portal profile](https://developer.dhl.com/user/apps) under your application (after access is approved).
- **Prerequisite:** Organisation must have an **active DHL Express customer account**. Sandbox/test is only for existing customers; non-customers cannot test the API without first opening an account.

### Notes
- Test environment has a daily limit (e.g. 500 service invocations) per credential set.
- Do not add a BOM to JSON request bodies.
- Moving to production: contact your DHL Express consultant.

---

## 2. Matkahuolto (M-Paketti XML interface)

### Official documentation
- **Interfaces (EN):** [Matkahuolto – Interfaces](https://www.matkahuolto.fi/corporate-customers/parcel-services/interfaces)
- **Shipment XML technical description (PDF):** [MHShipmentXML_EngV2.24.pdf](https://assets.ctfassets.net/lt6mvg8ztynj/2OaL3fp2yKgDiO7c1PVldB/b81f579f6631aedefea1189d9c368ffe/MHShipmentXML_EngV2.24.pdf)
- **Example response (XML):** [malli_vastaus.xml](https://assets.ctfassets.net/lt6mvg8ztynj/4HQB6wQfTrw5sn5k3xLeeb/fe254287798dbef3f45b3e6c72f1c8f4/malli_vastaus.xml)
- **Pickup point search (PDF):** [MHSearchOffices_EngV1.6.pdf](https://assets.ctfassets.net/lt6mvg8ztynj/2qtG4RKFf48a1PQm11UjQv/14dde546ca799c29470049cec17f9910/MHSearchOffices_EngV1.6.pdf)
- **Tracking (PDF):** [MHTracking_EngV1.3.pdf](https://assets.ctfassets.net/lt6mvg8ztynj/4q2klO2hq6XX3QvBDhmsAb/d919607de1c886788be4dc7c52d811b3/MHTracking_EngV1.3.pdf)

### Endpoints
- **Shipment (address label) request:** HTTPS **POST** to the URL provided by Matkahuolto (see below).
- **Protocol:** HTTPS only; no unencrypted HTTP.
- **Request/response:** XML; successful response can include address label in PDF format.

### Test vs production
- Matkahuolto provides a **testing environment** that returns correctly formatted replies; shipments sent there are **not** processed.
- **Production** is used by “changing the contact address” (i.e. the POST URL) as instructed by Matkahuolto.
- The exact test URL (e.g. `https://extservicestest.matkahuolto.fi/...`) is **not** published on the public interfaces page; it is given with your **Customer ID** and deployment instructions.

### Credentials and contact
- **Customer ID** (and test/production URLs) are **not** public; you must request them from Matkahuolto.
- **Request a free Customer ID and get test/production URLs:**  
  **Email:** [verkkokauppapalvelut@matkahuolto.fi](mailto:verkkokauppapalvelut@matkahuolto.fi)
- **Technical support / XML interface help:** same address: [verkkokauppapalvelut@matkahuolto.fi](mailto:verkkokauppapalvelut@matkahuolto.fi)

### Sample code (official)
- [Matkahuolto sample codes (PHP, C#, Java, Ruby)](https://www.matkahuolto.fi/corporate-customers/parcel-services/interfaces) – zip downloads on the same Interfaces page.

---

## 3. PostNord (tracking and shipment APIs)

### Official documentation and signup
- **Developer portal (general):** [Developer Portal – PostNord](https://developer.postnord.com/)
- **API signup (free plan, API key):** [PostNord API signup](https://postnord-ab-production.3scale.net/signup)  
  - Requires: Company name, email, country, username, password (min 6 chars), business owner details, team selection.  
  - Free plan has daily call limits.
- **Track & Trace / shipment APIs:** [PostNord Track Shipment API](https://www.postnord.com/integrations/track-shipment-api/)
- **Portal (SE):** [PostNord resources – Integrationer / API](https://portal.postnord.com/se/sv/resurser/integrationer/api)

### Environments
- **Production:** Often referenced as `api2.postnord.com` (e.g. `/rest/shipment/...`). Exact base URL and paths are in the API details for each product in the developer portal.
- **Test / sandbox:** [PostNord Developer Portal Sandbox](https://atdeveloper.postnord.com/) – use for testing; hostname may be `atapi2.postnord.com` for test APIs (confirm in the portal for each API).

### Typical tracking endpoint (concept)
- **Track and Trace:** e.g. `GET .../rest/shipment/v5/trackandtrace/findByIdentifier.json?apikey={apikey}&id={id}`  
  Exact path and parameters are in the [developer portal API details](https://developer.postnord.com/apis/details?systemName=shipment-v5-trackandtrace-shipmentinformation) (and similar pages for other APIs).

### Credentials
- **API key:** Obtained after signup at [postnord-ab-production.3scale.net/signup](https://postnord-ab-production.3scale.net/signup).  
- Typically passed as query parameter `apikey` or as per the specific API’s documentation.  
- Free tier available; limits and production access as per PostNord’s terms.

---

## 4. Kaukokiito (partner API)

### Official documentation
- **Public API docs:** Kaukokiito does **not** publish open developer documentation for the partner `createTransportOrder` (or similar) API on the web.
- **Known from booking-backend:**  
  - Production: `https://api.kaukokiito.fi/api/partner/createTransportOrder`  
  - Test: `https://test-api.kaukokiito.fi/api/partner/createTransportOrder`  
  - Auth: header `x-api-key` with an API key.

### How to get valid endpoints and credentials
- **Option 1 – Integration partner (Coneksion):** [Connect easily with Kaukokiito](https://www.coneksion.com/connect-easily-with-kaukokiito)  
  Coneksion is described as Kaukokiito’s integration partner for API connectivity (ordering, tracking, labels, invoicing). Sign up / contact via their form to get API access and credentials.
- **Option 2 – nShift:** [Kaukokiito Integration | nShift](https://nshift.com/en/carriers/kaukokiito)  
  nShift is another integration partner; contact their carrier team for integration options.
- **Direct:** Contact Kaukokiito (e.g. via [kaukokiito.fi](https://www.kaukokiito.fi/en)) and ask for **partner API / developer access**; they will typically refer you to a partner or provide credentials under an agreement.

### Notes
- Keys found in existing code (e.g. prod/test API keys) may be account-specific or rotated; 401 responses mean new valid keys must be obtained from Kaukokiito or their integration partner.

---

## 5. SMTP / email-based couriers (e.g. Hämeen Tavarataxi)

- **No “courier API” to document:** Notifications are sent by **email** (e.g. via SMTP).
- **Configuration:** Use your own or company SMTP server (e.g. `smtp.example.com`, port 465, TLS) and the mailbox credentials (e.g. from booking-backend `.env`: `INFO_EMAIL_ADDRESS`, `INFO_EMAIL_PASSWORD`, `HAMEEN_TAVARA_TAXI_EMAIL`).
- **Valid “credentials”:** Valid SMTP host, port, user, password, and recipient addresses; no separate “API key” from the carrier.

---

## Summary table

| Courier | Official docs / signup | Credentials | Endpoints |
|--------|-------------------------|------------|-----------|
| **DHL Express** | [developer.dhl.com](https://developer.dhl.com/api-reference/dhl-express-mydhl-api) | Consultant or Developer Portal (API Key + Secret), Basic auth | Test: `https://express.api.dhl.com/mydhlapi/test` + `/shipments`, `/shipments/{id}/tracking` |
| **Matkahuolto** | [matkahuolto.fi/interfaces](https://www.matkahuolto.fi/corporate-customers/parcel-services/interfaces) + PDF | Customer ID + test/prod URL from [verkkokauppapalvelut@matkahuolto.fi](mailto:verkkokauppapalvelut@matkahuolto.fi) | HTTPS POST URL provided by Matkahuolto (test URL not published) |
| **PostNord** | [developer.postnord.com](https://developer.postnord.com/), [signup](https://postnord-ab-production.3scale.net/signup) | API key from signup | See developer portal; e.g. track: `.../trackandtrace/findByIdentifier.json?apikey=...` |
| **Kaukokiito** | No public API docs | API key from Kaukokiito or partner (Coneksion / nShift) | e.g. `api.kaukokiito.fi` / `test-api.kaukokiito.fi` + `/api/partner/createTransportOrder` |
| **Email (e.g. Hämeen Tavarataxi)** | N/A | SMTP server + mailbox credentials | Your SMTP host:port and recipient addresses |

Use this document to align CargoHub and booking-backend with **official** endpoints and to request or rotate **valid** credentials from each provider.
