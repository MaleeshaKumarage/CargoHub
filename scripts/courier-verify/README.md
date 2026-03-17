# Courier endpoint verification (curl)

Credentials and URLs here are from booking-backend (DHL hardcoded in code, Matkahuolto from controller).  
**For official endpoints and how to obtain valid credentials**, see [Courier-API-Official-Documentation.md](../../docs/Courier-API-Official-Documentation.md).

## Verification results (last run)

| Endpoint | Result | Notes |
|----------|--------|--------|
| **DHL Express – Create** | OK | 422 until body is valid (e.g. remove `reference`, set `plannedShippingDateAndTime` within next 10 days). Credentials accepted. |
| **DHL Express – Tracking** | OK | 404 for unknown ID = endpoint + auth valid. |
| **Matkahuolto – Create** | Timeout | Host `extservicestest.matkahuolto.fi` responds (302 on GET). POST to `/mpaketti/mhshipmentxml` timed out (45s). May need VPN, firewall allowlist, or different network. |
| **SMTP (Hämeen Tavarataxi)** | Not tested via curl | Use app or script with `mail.hiava.fi:465` and credentials from .env. |
| **Postnord (test API)** | OK | Tracking URL + test apikey return 200 (business error for invalid id). Endpoint and key valid. |
| **Kaukokiito (prod URL)** | 401 | Endpoint reachable; API key from booking-backend code rejected. Use test key or get valid key. |

---

## 1. DHL Express (test API)

- **Create shipment:** `POST https://express.api.dhl.com/mydhlapi/test/shipments`
- **Tracking:** `GET https://express.api.dhl.com/mydhlapi/test/shipments/{shipmentId}/tracking`
- **Auth:** Basic (from booking-backend: `hiavanetoyFI:X@4xB!7hV^2dK#3l`)

```bash
# Base64 for Basic auth (run once to get token)
# PowerShell: [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes("hiavanetoyFI:X@4xB!7hV^2dK#3l"))

curl -s -w "\nHTTP_CODE:%{http_code}\n" -X POST "https://express.api.dhl.com/mydhlapi/test/shipments?strictValidation=false&bypassPLTError=false&validateDataOnly=false" ^
  -H "Authorization: Basic aGlhdmFuZXRveUZJOlhANHhCITdoVl4yZEsjM2w=" ^
  -H "Content-Type: application/json" ^
  -d @dhl-express-test.json
```

## 2. Matkahuolto (test)

- **URL:** `POST https://extservicestest.matkahuolto.fi/mpaketti/mhshipmentxml`
- **Auth:** None in URL; credentials in XML body (UserId 9430023, Password 456 from booking-backend controller).

```bash
curl -s -w "\nHTTP_CODE:%{http_code}\n" -X POST "https://extservicestest.matkahuolto.fi/mpaketti/mhshipmentxml" ^
  -H "Content-Type: text/xml; charset=utf-8" ^
  -d @matkahuolto-test.xml
```

## 3. Postnord (test API – tracking)

```bash
curl -s -w "\nHTTP_CODE:%{http_code}\n" "https://atapi2.postnord.com/rest/shipment/v5/trackandtrace/findByIdentifier.json?apikey=08c66df232fce17fae98d032c7fe4164&id=FAKE123"
```
Expected: 200 with `invalidIdentifier` in body = endpoint and test apikey valid.

## 4. Kaukokiito (production URL)

API key in booking-backend code may be test-only or revoked. Test URL: `https://test-api.kaukokiito.fi/api/partner/createTransportOrder` with test key `9862e7cb-8418-49e8-8523-a43ae07a0e93`.

```bash
curl -s -w "\nHTTP_CODE:%{http_code}\n" -X POST "https://api.kaukokiito.fi/api/partner/createTransportOrder" -H "Content-Type: application/json" -H "x-api-key: YOUR_API_KEY" -d "{}"
```

## 5. SMTP (Hämeen Tavarataxi email)

Not verifiable via curl in the same way. Use a small .NET or script to send one test email with `mail.hiava.fi:465` and `noreply@hiava.fi` / password from .env.

## Expected outcomes

- **DHL Create:** 200/201 = success; 401 = wrong credentials; 422 = validation (e.g. date must be within next 10 days, no top-level `reference`). Endpoint and credentials are valid if you get 422.
- **DHL Tracking:** 200 = success; 404 = shipment not found (endpoint and auth OK).
- **Matkahuolto:** 200 + XML = success; timeout = network/firewall or service slow; 4xx = auth or validation.
