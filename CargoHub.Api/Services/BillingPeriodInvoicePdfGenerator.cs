using CargoHub.Application.Billing.Admin;
using CargoHub.Application.Billing.AdminInvoicing;
using CargoHub.Api.Options;
using Microsoft.Extensions.Options;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CargoHub.Api.Services;

public sealed class BillingPeriodInvoicePdfGenerator : IBillingInvoicePdfGenerator
{
    private readonly BrandingOptions _branding;

    public BillingPeriodInvoicePdfGenerator(IOptions<BrandingOptions> branding)
    {
        _branding = branding?.Value ?? new BrandingOptions();
    }

    public byte[] GeneratePdf(BillingInvoicePdfModel model)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var title = !string.IsNullOrWhiteSpace(_branding.AppName) ? $"{_branding.AppName} — Invoice" : "Billing invoice";

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.PageColor(Colors.White);

                page.Header().Column(column =>
                {
                    column.Item().Text(title).Bold().FontSize(20).FontColor(Colors.Blue.Darken2);
                    column.Item().PaddingTop(4).LineHorizontal(1).LineColor(Colors.Grey.Medium);
                });

                page.Content().PaddingTop(16).Column(content =>
                {
                    content.Spacing(10);

                    content.Item().Text($"{model.YearUtc}-{model.MonthUtc:00} (UTC)").SemiBold().FontSize(12);
                    content.Item().Text($"Company: {model.CompanyName}").FontSize(11);
                    if (!string.IsNullOrWhiteSpace(model.BusinessId))
                        content.Item().Text($"Business ID: {model.BusinessId}").FontSize(10).FontColor(Colors.Grey.Darken2);
                    content.Item().Text($"Status: {model.Status}  ·  Currency: {model.Currency}").FontSize(10);

                    content.Item().PaddingTop(8).Row(row =>
                    {
                        row.RelativeItem().Text($"Ledger total: {model.LedgerTotal:N2} {model.Currency}").FontSize(11);
                        row.RelativeItem().AlignRight().Text($"Amount due: {model.PayableTotal:N2} {model.Currency}")
                            .SemiBold().FontSize(11);
                    });

                    content.Item().PaddingTop(12).Text("Line items").Bold().FontSize(12);

                    content.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(1);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(CellHeader).Text("Type");
                            header.Cell().Element(CellHeader).Text("Component");
                            header.Cell().Element(CellHeader).AlignRight().Text("Amount");
                            header.Cell().Element(CellHeader).Text("On invoice");
                        });

                        foreach (var line in model.Lines)
                        {
                            table.Cell().Element(Cell).Text(line.LineType);
                            table.Cell().Element(Cell).Text(line.Component ?? "—");
                            table.Cell().Element(Cell).AlignRight().Text($"{line.Amount:N2}");
                            table.Cell().Element(Cell).Text(line.ExcludedFromInvoice ? "No" : "Yes");
                        }
                    });
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.DefaultTextStyle(x => x.FontSize(9).FontColor(Colors.Grey.Medium));
                    text.Span("Generated ");
                    text.Span(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm") + " UTC");
                });
            });
        });

        return doc.GeneratePdf();
    }

    private static IContainer CellHeader(IContainer c) =>
        c.DefaultTextStyle(x => x.SemiBold().FontSize(9)).PaddingVertical(4).BorderBottom(1).BorderColor(Colors.Grey.Lighten2);

    private static IContainer Cell(IContainer c) =>
        c.DefaultTextStyle(x => x.FontSize(9)).PaddingVertical(3).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten3);
}
