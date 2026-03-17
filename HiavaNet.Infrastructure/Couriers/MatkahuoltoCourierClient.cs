using System.Text;
using System.Xml.Linq;
using HiavaNet.Application.Couriers;
using Microsoft.Extensions.Options;

namespace HiavaNet.Infrastructure.Couriers;

/// <summary>
/// Matkahuolto integration via XML over HTTP.
/// Aligns with booking-backend matkahuolto/matkahuolto-service.ts.
/// </summary>
public sealed class MatkahuoltoCourierClient : ICourierBookingClient
{
    public const string CourierId = "Matkahuolto";
    public const string HttpClientName = "Courier.Matkahuolto";
    string ICourierBookingClient.CourierId => CourierId;

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly MatkahuoltoOptions _options;

    public MatkahuoltoCourierClient(IHttpClientFactory httpClientFactory, IOptions<MatkahuoltoOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _options = options?.Value ?? new MatkahuoltoOptions();
    }

    private HttpClient GetClient() => _httpClientFactory.CreateClient(HttpClientName);

    public async Task<CourierCreateResult> CreateBookingAsync(
        CourierCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_options.BookingUrl))
            return new CourierCreateResult { Success = false, Message = "Matkahuolto BookingUrl not configured." };

        try
        {
            var xml = BuildRequestXml(request);
            var content = new StringContent(xml, Encoding.UTF8, "text/xml");
            var response = await GetClient().PostAsync(_options.BookingUrl, content, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
                return new CourierCreateResult { Success = false, Message = $"Matkahuolto returned {response.StatusCode}: {responseBody}" };

            return ParseMatkahuoltoResponse(responseBody);
        }
        catch (Exception ex)
        {
            return new CourierCreateResult { Success = false, Message = ex.Message };
        }
    }

    public Task<CourierStatusResult?> GetStatusAsync(
        string carrierShipmentIdOrReference,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<CourierStatusResult?>(null);
    }

    private static string BuildRequestXml(CourierCreateRequest request)
    {
        var ns = XNamespace.Get("http://www.matkahuolto.fi/mhshipmentxml");
        var doc = new XDocument(
            new XElement(ns + "MHShipment",
                new XElement(ns + "Shipment",
                    new XElement(ns + "Sender",
                        new XElement(ns + "Name", request.Shipper.Name),
                        new XElement(ns + "Address", request.Shipper.Address1),
                        new XElement(ns + "PostCode", request.Shipper.PostalCode),
                        new XElement(ns + "City", request.Shipper.City),
                        new XElement(ns + "Country", request.Shipper.Country),
                        new XElement(ns + "Phone", request.Shipper.PhoneNumber ?? ""),
                        new XElement(ns + "Email", request.Shipper.Email ?? "")
                    ),
                    new XElement(ns + "Receiver",
                        new XElement(ns + "Name", request.Receiver.Name),
                        new XElement(ns + "Address", request.Receiver.Address1),
                        new XElement(ns + "PostCode", request.Receiver.PostalCode),
                        new XElement(ns + "City", request.Receiver.City),
                        new XElement(ns + "Country", request.Receiver.Country),
                        new XElement(ns + "Phone", request.Receiver.PhoneNumber ?? ""),
                        new XElement(ns + "Email", request.Receiver.Email ?? "")
                    ),
                    new XElement(ns + "Reference", request.ShipmentNumber),
                    new XElement(ns + "Parcels",
                        request.Packages.Select(p => new XElement(ns + "Parcel",
                            new XElement(ns + "Weight", p.Weight ?? "1"),
                            new XElement(ns + "Volume", p.Volume ?? "0.001")
                        ))
                    )
                )
            )
        );
        return doc.ToString(SaveOptions.DisableFormatting);
    }

    private static CourierCreateResult ParseMatkahuoltoResponse(string xmlResponse)
    {
        try
        {
            var root = XElement.Parse(xmlResponse);
            var ns = root.Name.Namespace;
            var reply = root.Element(ns + "MHShipmentReply") ?? root;
            var shipment = reply.Element(ns + "Shipment");
            var shipmentNumber = shipment?.Element(ns + "ShipmentNumber")?.Value ?? "";
            var pdfEl = reply.Element(ns + "ShipmentPdf");
            var labelBase64 = pdfEl?.Value ?? "";

            return new CourierCreateResult
            {
                Success = !string.IsNullOrEmpty(shipmentNumber),
                CarrierShipmentId = shipmentNumber,
                TrackingNumber = shipmentNumber,
                LabelPdfBase64 = string.IsNullOrEmpty(labelBase64) ? null : labelBase64,
            };
        }
        catch
        {
            return new CourierCreateResult { Success = false, Message = "Failed to parse Matkahuolto XML response." };
        }
    }
}

/// <summary>
/// Configuration for Matkahuolto. Bind from Courier:Matkahuolto.
/// </summary>
public class MatkahuoltoOptions
{
    public const string SectionName = "Courier:Matkahuolto";
    public string? BookingUrl { get; set; } = "https://extservicestest.matkahuolto.fi/mpaketti/mhshipmentxml";
}
