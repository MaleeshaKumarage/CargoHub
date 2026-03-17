using System.Net.Http.Json;
using System.Text.Json;
using CargoHub.Application.Couriers;
using Microsoft.Extensions.Options;

namespace CargoHub.Infrastructure.Couriers;

/// <summary>
/// DHL Express integration via REST API (JSON).
/// Aligns with booking-backend dhl/dhlexpress-service.ts.
/// </summary>
public sealed class DhlExpressCourierClient : ICourierBookingClient
{
    public const string CourierId = "DHLExpress";
    public const string HttpClientName = "Courier.DhlExpress";
    string ICourierBookingClient.CourierId => CourierId;

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly DhlExpressOptions _options;

    public DhlExpressCourierClient(IHttpClientFactory httpClientFactory, IOptions<DhlExpressOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _options = options?.Value ?? new DhlExpressOptions();
    }

    private HttpClient GetClient() => _httpClientFactory.CreateClient(HttpClientName);

    public async Task<CourierCreateResult> CreateBookingAsync(
        CourierCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_options.BaseUrl))
            return new CourierCreateResult { Success = false, Message = "DHL Express BaseUrl not configured." };

        try
        {
            var payload = MapToDhlPayload(request);
            var url = _options.UseTest ? _options.TestCreateUrl : _options.CreateUrl;
            if (string.IsNullOrEmpty(url)) url = _options.BaseUrl.TrimEnd('/') + "/shipments";

            var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.TryAddWithoutValidation("Authorization", "Basic " + _options.BasicAuthBase64);
            req.Content = JsonContent.Create(payload);

            var response = await GetClient().SendAsync(req, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
                return new CourierCreateResult { Success = false, Message = $"DHL returned {response.StatusCode}: {body}" };

            var doc = JsonDocument.Parse(body);
            var shipmentId = doc.RootElement.TryGetProperty("shipmentTrackingNumber", out var st) ? st.GetString() : null;
            var labels = new List<string>();
            if (doc.RootElement.TryGetProperty("documents", out var docs))
                foreach (var d in docs.EnumerateArray())
                    if (d.TryGetProperty("content", out var c))
                        labels.Add(c.GetString() ?? "");

            return new CourierCreateResult
            {
                Success = true,
                CarrierShipmentId = shipmentId,
                TrackingNumber = shipmentId,
                LabelPdfBase64 = labels.Count > 0 ? labels[0] : null,
            };
        }
        catch (Exception ex)
        {
            return new CourierCreateResult { Success = false, Message = ex.Message };
        }
    }

    public async Task<CourierStatusResult?> GetStatusAsync(
        string carrierShipmentIdOrReference,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_options.BaseUrl))
            return null;

        try
        {
            var url = _options.UseTest ? _options.TestTrackingUrl : _options.TrackingUrl;
            if (string.IsNullOrEmpty(url)) url = $"{_options.BaseUrl.TrimEnd('/')}/shipments/{carrierShipmentIdOrReference}/tracking";

            var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.TryAddWithoutValidation("Authorization", "Basic " + _options.BasicAuthBase64);

            var response = await GetClient().SendAsync(req, cancellationToken);
            if (!response.IsSuccessStatusCode) return null;

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            var doc = JsonDocument.Parse(body);
            var status = doc.RootElement.TryGetProperty("status", out var s) ? s.GetString() : null;
            var events = new List<CourierStatusEventDto>();
            if (doc.RootElement.TryGetProperty("tracking", out var tr) && tr.ValueKind == JsonValueKind.Array)
                foreach (var e in tr.EnumerateArray())
                {
                    var desc = e.TryGetProperty("description", out var d) ? d.GetString() : null;
                    var time = e.TryGetProperty("date", out var dt) ? dt.GetString() : null;
                    events.Add(new CourierStatusEventDto
                    {
                        Description = desc,
                        OccurredAtUtc = DateTime.TryParse(time, null, System.Globalization.DateTimeStyles.RoundtripKind, out var t) ? t : null
                    });
                }

            return new CourierStatusResult
            {
                CarrierShipmentId = carrierShipmentIdOrReference,
                StatusCode = status,
                StatusDescription = status,
                Events = events,
            };
        }
        catch
        {
            return null;
        }
    }

    private static object MapToDhlPayload(CourierCreateRequest request)
    {
        return new
        {
            plannedShippingDateAndTime = request.Shipment.ShipmentDateTime ?? DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            pickup = new
            {
                isRequested = false
            },
            productCode = request.Service ?? "N",
            accounts = new[] { new { number = "0", typeCode = "shipper" } },
            valueAddedServices = Array.Empty<object>(),
            outputImageProperties = new { printerDPI = 300, encodingFormat = "pdf", imageOptions = new[] { new { typeCode = "label" } } },
            customerDetails = new
            {
                shipperDetails = new
                {
                    postalAddress = new
                    {
                        streetLines = new[] { request.Shipper.Address1 },
                        city = request.Shipper.City,
                        postalCode = request.Shipper.PostalCode,
                        countryCode = MapCountry(request.Shipper.Country)
                    },
                    contactInformation = new
                    {
                        email = request.Shipper.Email,
                        phone = request.Shipper.PhoneNumber,
                        companyName = request.Shipper.Name,
                        fullName = request.Shipper.ContactPersonName ?? request.Shipper.Name
                    }
                },
                receiverDetails = new
                {
                    postalAddress = new
                    {
                        streetLines = new[] { request.Receiver.Address1 },
                        city = request.Receiver.City,
                        postalCode = request.Receiver.PostalCode,
                        countryCode = MapCountry(request.Receiver.Country)
                    },
                    contactInformation = new
                    {
                        email = request.Receiver.Email,
                        phone = request.Receiver.PhoneNumber,
                        companyName = request.Receiver.Name,
                        fullName = request.Receiver.ContactPersonName ?? request.Receiver.Name
                    }
                }
            },
            documents = Array.Empty<object>(),
            shipmentDetails = new
            {
                numberOfPieces = request.Packages.Count,
                weight = (int)(request.Packages.Sum(p => double.TryParse(p.Weight, System.Globalization.NumberStyles.Any, null, out var w) ? w : 0) * 1000),
                weightUnit = "kg",
                globalProductCode = "N",
                localProductCode = "N",
                packages = request.Packages.Select((p, i) => new
                {
                    weight = double.TryParse(p.Weight, System.Globalization.NumberStyles.Any, null, out var w) ? w : 1,
                    dimensions = new
                    {
                        length = double.TryParse(p.Length, System.Globalization.NumberStyles.Any, null, out var l) ? (int)l : 1,
                        width = double.TryParse(p.Width, System.Globalization.NumberStyles.Any, null, out var wd) ? (int)wd : 1,
                        height = double.TryParse(p.Height, System.Globalization.NumberStyles.Any, null, out var h) ? (int)h : 1
                    }
                }).ToArray()
            },
            reference = request.ShipmentNumber
        };
    }

    private static string MapCountry(string country)
    {
        if (string.IsNullOrWhiteSpace(country)) return "FI";
        if (country.Length == 2) return country.ToUpperInvariant();
        return country.ToUpperInvariant() switch
        {
            "FINLAND" => "FI",
            "SWEDEN" => "SE",
            "NORWAY" => "NO",
            "DENMARK" => "DK",
            _ => country.Length >= 2 ? country[..2].ToUpperInvariant() : "FI"
        };
    }
}

/// <summary>
/// Configuration for DHL Express. Bind from Courier:DHLExpress or environment.
/// </summary>
public class DhlExpressOptions
{
    public const string SectionName = "Courier:DHLExpress";
    public string BaseUrl { get; set; } = "https://express.api.dhl.com/mydhlapi";
    public bool UseTest { get; set; } = true;
    public string? TestCreateUrl { get; set; }
    public string? TestTrackingUrl { get; set; }
    public string? CreateUrl { get; set; }
    public string? TrackingUrl { get; set; }
    /// <summary>Base64 of "username:password" for Basic auth.</summary>
    public string? BasicAuthBase64 { get; set; }
}
