using CargoHub.Api.Options;
using CargoHub.Application.Bookings;
using Microsoft.Extensions.Options;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CargoHub.Api.Services;

/// <summary>Tabular PDF of bookings for the daily admin digest.</summary>
public sealed class DailyBookingsDigestPdfGenerator
{
    private readonly BrandingOptions _branding;

    public DailyBookingsDigestPdfGenerator(IOptions<BrandingOptions> branding)
    {
        _branding = branding?.Value ?? new BrandingOptions();
    }

    public byte[] Generate(string companyName, DateOnly digestDate, string timeZoneId, IReadOnlyList<DailyDigestPdfRow> rows)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var title = !string.IsNullOrWhiteSpace(_branding.AppName) ? _branding.AppName : "CargoHub";

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(1.2f, Unit.Centimetre);
                page.PageColor(Colors.White);

                page.Header().Column(col =>
                {
                    col.Item().Text("Daily bookings digest").Bold().FontSize(18).FontColor(Colors.Blue.Darken2);
                    col.Item().PaddingTop(4).Text($"{companyName} — {digestDate:yyyy-MM-dd} ({timeZoneId})").SemiBold().FontSize(11);
                    col.Item().PaddingTop(6).LineHorizontal(1).LineColor(Colors.Grey.Medium);
                });

                page.Content().PaddingTop(12).Column(content =>
                {
                    if (rows.Count == 0)
                    {
                        content.Item().Text("No bookings in this period.").Italic().FontColor(Colors.Grey.Medium);
                        return;
                    }

                    content.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(70);
                            c.RelativeColumn(1.1f);
                            c.RelativeColumn(1.2f);
                            c.RelativeColumn(0.9f);
                            c.RelativeColumn(1.1f);
                            c.RelativeColumn(1f);
                            c.RelativeColumn(0.9f);
                            c.RelativeColumn(0.7f);
                            c.RelativeColumn(0.9f);
                            c.RelativeColumn(0.9f);
                        });

                        static IContainer CellStyle(IContainer c) => c.BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(4);

                        table.Header(h =>
                        {
                            void H(string label) => h.Cell().Element(CellStyle).Background(Colors.Grey.Lighten3).Text(label).Bold().FontSize(7);
                            H("Booking ID");
                            H("Courier");
                            H("Receiver");
                            H("City");
                            H("Created");
                            H("Created by");
                            H("Status");
                            H("Draft");
                            H("Ref");
                            H("Waybill");
                        });

                        foreach (var r in rows)
                        {
                            void D(string text) => table.Cell().Element(CellStyle).Text(text).FontSize(7);
                            D(r.BookingId.ToString("N")[..8] + "…");
                            D(Truncate(r.Courier, 28));
                            D(Truncate(r.ReceiverName, 28));
                            D(Truncate(r.City, 20));
                            D(r.CreatedAtDisplay);
                            D(Truncate(r.CreatedBy, 22));
                            D(Truncate(r.Status, 18));
                            D(r.IsDraft ? "Yes" : "No");
                            D(Truncate(r.Reference ?? "—", 14));
                            D(Truncate(r.Waybill ?? "—", 14));
                        }
                    });
                });

                page.Footer().AlignCenter().Text($"{title} — digest generated {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC")
                    .FontSize(7).FontColor(Colors.Grey.Medium);
            });
        });

        return doc.GeneratePdf();
    }

    private static string Truncate(string s, int max)
    {
        if (string.IsNullOrEmpty(s)) return "—";
        s = s.Trim();
        return s.Length <= max ? s : s[..(max - 1)] + "…";
    }
}
