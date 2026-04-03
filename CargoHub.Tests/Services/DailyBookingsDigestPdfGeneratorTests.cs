using CargoHub.Api.Options;
using CargoHub.Api.Services;
using CargoHub.Application.Bookings;
using Microsoft.Extensions.Options;
using Xunit;

namespace CargoHub.Tests.Services;

public class DailyBookingsDigestPdfGeneratorTests
{
    [Fact]
    public void Generate_WithRows_ProducesNonEmptyPdf()
    {
        var gen = new DailyBookingsDigestPdfGenerator(Options.Create(new BrandingOptions { AppName = "TestApp" }));
        var rows = new List<DailyDigestPdfRow>
        {
            new()
            {
                BookingId = Guid.NewGuid(),
                Courier = "DHL",
                ReceiverName = "Jane Doe",
                City = "Helsinki",
                CreatedAtDisplay = "2025-06-01 10:00",
                CreatedBy = "user-1",
                Status = "CompletedBooking",
                Reference = "REF-1",
                Waybill = "WB-1",
                IsDraft = false
            }
        };

        var pdf = gen.Generate("Acme Oy", new DateOnly(2025, 6, 1), "UTC", rows);

        Assert.NotNull(pdf);
        Assert.True(pdf.Length > 500);
        Assert.Equal(0x25, pdf[0]); // PDF starts with %
    }

    [Fact]
    public void Generate_EmptyRows_StillProducesPdf()
    {
        var gen = new DailyBookingsDigestPdfGenerator(Options.Create(new BrandingOptions()));
        var pdf = gen.Generate("Co", new DateOnly(2025, 1, 1), "UTC", Array.Empty<DailyDigestPdfRow>());
        Assert.True(pdf.Length > 200);
    }
}
