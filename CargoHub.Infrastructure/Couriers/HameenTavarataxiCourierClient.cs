using System.Text;
using CargoHub.Application.Couriers;
using Microsoft.Extensions.Options;

namespace CargoHub.Infrastructure.Couriers;

/// <summary>
/// Hämeen Tavarataxi integration via email (no API).
/// Aligns with booking-backend mailServices/hämeenTavarataxiMailService.ts.
/// </summary>
public sealed class HameenTavarataxiCourierClient : ICourierBookingClient
{
    public const string CourierId = "HämeenTavarataxi";
    string ICourierBookingClient.CourierId => CourierId;

    private readonly IEmailSender _emailSender;
    private readonly HameenTavarataxiOptions _options;

    public HameenTavarataxiCourierClient(IEmailSender emailSender, IOptions<HameenTavarataxiOptions> options)
    {
        _emailSender = emailSender;
        _options = options?.Value ?? new HameenTavarataxiOptions();
    }

    public async Task<CourierCreateResult> CreateBookingAsync(
        CourierCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        var to = request.IsTestBooking || string.IsNullOrEmpty(_options.CarrierEmail)
            ? _options.TestEmail ?? "test@example.com"
            : _options.CarrierEmail;

        var subject = $"Booking created at {DateTime.UtcNow:yyyy-MM-dd}";
        var html = BuildBookingEmailHtml(request);

        try
        {
            await _emailSender.SendAsync(to, subject, html, cancellationToken);
            return new CourierCreateResult
            {
                Success = true,
                CarrierShipmentId = null,
                Message = "Booking notification sent by email.",
            };
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

    private static string BuildBookingEmailHtml(CourierCreateRequest request)
    {
        var sb = new StringBuilder();
        sb.Append("<html><body>");
        sb.Append("Hello,<br><br>We would like to inform you that a new shipment order has been placed.<br><br>");
        sb.Append($"<strong>Shipment Number:</strong> {Escape(request.ShipmentNumber)}<br><br>");
        sb.Append("<strong>Shipper (Pickup):</strong><br>");
        sb.Append($"Name: {Escape(request.Shipper.Name)}<br>");
        sb.Append($"Address: {Escape(request.Shipper.Address1)}, {Escape(request.Shipper.PostalCode)} {Escape(request.Shipper.City)}, {Escape(request.Shipper.Country)}<br>");
        sb.Append($"Contact: {Escape(request.Shipper.ContactPersonName)} | {Escape(request.Shipper.PhoneNumber)} | {Escape(request.Shipper.Email)}<br>");
        sb.Append($"Pickup window: {Escape(request.Shipment.PickUpTimeEarliest)} - {Escape(request.Shipment.PickUpTimeLatest)}<br><br>");
        sb.Append("<strong>Receiver (Delivery):</strong><br>");
        sb.Append($"Name: {Escape(request.Receiver.Name)}<br>");
        sb.Append($"Address: {Escape(request.Receiver.Address1)}, {Escape(request.Receiver.PostalCode)} {Escape(request.Receiver.City)}, {Escape(request.Receiver.Country)}<br>");
        sb.Append($"Contact: {Escape(request.Receiver.ContactPersonName)} | {Escape(request.Receiver.PhoneNumber)} | {Escape(request.Receiver.Email)}<br>");
        sb.Append($"Delivery window: {Escape(request.Shipment.DeliveryTimeEarliest)} - {Escape(request.Shipment.DeliveryTimeLatest)}<br><br>");
        sb.Append($"<strong>Pickup instructions:</strong> {Escape(request.ShippingInfo.PickupHandlingInstructions)}<br>");
        sb.Append($"<strong>Load meter:</strong> {Escape(request.ShippingInfo.LoadMeter)}<br>");
        sb.Append($"<strong>Gross weight:</strong> {Escape(request.ShippingInfo.GrossWeight)} kg | <strong>Gross volume:</strong> {Escape(request.ShippingInfo.GrossVolume)} m³<br>");
        sb.Append($"<strong>Number of packages:</strong> {request.Packages.Count}<br><br>");
        sb.Append("<strong>Package details:</strong><br><ul>");
        foreach (var p in request.Packages)
        {
            sb.Append($"<li>Weight: {Escape(p.Weight)} kg, Volume: {Escape(p.Volume)} m³, Description: {Escape(p.Description)}</li>");
        }
        sb.Append("</ul><br>Best regards,<br>CargoHub Team</body></html>");
        return sb.ToString();
    }

    private static string Escape(string? s) => string.IsNullOrEmpty(s) ? "N/A" : System.Net.WebUtility.HtmlEncode(s);
}

/// <summary>
/// Configuration for Hämeen Tavarataxi email integration.
/// </summary>
public class HameenTavarataxiOptions
{
    public const string SectionName = "Courier:HameenTavarataxi";
    public string? CarrierEmail { get; set; }
    public string? TestEmail { get; set; }
}
