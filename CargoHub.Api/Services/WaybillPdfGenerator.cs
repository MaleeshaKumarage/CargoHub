using CargoHub.Api.Options;
using CargoHub.Application.Bookings.Dtos;
using Microsoft.Extensions.Options;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CargoHub.Api.Services;

/// <summary>
/// Generates a waybill PDF for a booking. Footer text comes from branding config.
/// </summary>
public class WaybillPdfGenerator
{
    private readonly BrandingOptions _branding;

    public WaybillPdfGenerator(IOptions<BrandingOptions> branding)
    {
        _branding = branding?.Value ?? new BrandingOptions();
    }

    public byte[] Generate(BookingDetailDto booking)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var waybillNumber = booking.WaybillNumber ?? $"WB-{booking.Id:N}"[..15];
        var refNumber = booking.Header?.ReferenceNumber ?? booking.Id.ToString("N")[..8];
        var shipper = booking.Shipper;
        var receiver = booking.Receiver;
        var footerText = !string.IsNullOrWhiteSpace(_branding.WaybillFooterText)
            ? _branding.WaybillFooterText
            : (!string.IsNullOrWhiteSpace(_branding.AppName) ? $"{_branding.AppName} — Waybill" : "Waybill");

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.PageColor(Colors.White);

                page.Header().Column(column =>
                {
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Text("WAYBILL").Bold().FontSize(24).FontColor(Colors.Blue.Darken2);
                        row.RelativeItem().AlignRight().Text(waybillNumber).SemiBold().FontSize(14);
                    });
                    column.Item().PaddingTop(4).LineHorizontal(1).LineColor(Colors.Grey.Medium);
                });

                page.Content().PaddingTop(12).Column(content =>
                {
                    content.Spacing(12);

                    content.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Sender (Shipper)").Bold().FontSize(11);
                            c.Item().PaddingTop(2).Text(shipper?.Name ?? "—");
                            c.Item().Text(shipper != null ? $"{shipper.Address1}, {shipper.PostalCode} {shipper.City}, {shipper.Country}" : "—");
                            if (!string.IsNullOrEmpty(shipper?.Email)) c.Item().Text(shipper.Email);
                            if (!string.IsNullOrEmpty(shipper?.PhoneNumber)) c.Item().Text(shipper.PhoneNumber);
                        });
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Receiver").Bold().FontSize(11);
                            c.Item().PaddingTop(2).Text(receiver?.Name ?? "—");
                            c.Item().Text(receiver != null ? $"{receiver.Address1}, {receiver.PostalCode} {receiver.City}, {receiver.Country}" : "—");
                            if (!string.IsNullOrEmpty(receiver?.Email)) c.Item().Text(receiver.Email);
                            if (!string.IsNullOrEmpty(receiver?.PhoneNumber)) c.Item().Text(receiver.PhoneNumber);
                        });
                    });

                    content.Item().PaddingTop(8).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);
                    content.Item().Row(row =>
                    {
                        row.RelativeItem().Text($"Reference: {refNumber}").FontSize(10);
                        row.RelativeItem().Text($"Shipment: {booking.ShipmentNumber ?? "—"}").FontSize(10);
                        row.RelativeItem().Text($"Service: {booking.Header?.PostalService ?? "—"}").FontSize(10);
                    });
                    content.Item().Row(row =>
                    {
                        row.RelativeItem().Text($"Created: {booking.CreatedAtUtc:yyyy-MM-dd HH:mm} UTC").FontSize(9).FontColor(Colors.Grey.Medium);
                    });

                    if (booking.Packages != null && booking.Packages.Count > 0)
                    {
                        content.Item().PaddingTop(8).Text("Packages").Bold().FontSize(11);
                        content.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(30);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });
                            table.Header(header =>
                            {
                                header.Cell().Padding(4).Background(Colors.Grey.Lighten3).Text("#").Bold();
                                header.Cell().Padding(4).Background(Colors.Grey.Lighten3).Text("Weight").Bold();
                                header.Cell().Padding(4).Background(Colors.Grey.Lighten3).Text("Volume").Bold();
                                header.Cell().Padding(4).Background(Colors.Grey.Lighten3).Text("Type").Bold();
                            });
                            for (var i = 0; i < booking.Packages.Count; i++)
                            {
                                var p = booking.Packages[i];
                                var idx = i + 1;
                                table.Cell().Padding(4).Text(idx.ToString());
                                table.Cell().Padding(4).Text(p.Weight ?? "—");
                                table.Cell().Padding(4).Text(p.Volume ?? "—");
                                table.Cell().Padding(4).Text(p.PackageType ?? "—");
                            }
                        });
                    }
                });

                page.Footer()
                    .AlignCenter()
                    .Text(footerText)
                    .FontSize(8).FontColor(Colors.Grey.Medium);
            });
        });

        return doc.GeneratePdf();
    }
}
