using System.Text;
using CargoHub.Api.Services;
using Xunit;

namespace CargoHub.Tests.Bookings;

public class BookingImportServiceTests
{
    private readonly BookingImportService _service = new();

    [Fact]
    public void Parse_WhenHeadersInvalid_ReturnsError()
    {
        var csv = "WrongHeader1,WrongHeader2\nval1,val2";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var error = _service.Parse(stream, "test.csv", out var rows, out var skipped);

        Assert.NotNull(error);
        Assert.Equal(0, skipped);
        Assert.Contains("Header", error);
        Assert.Empty(rows);
    }

    [Fact]
    public void Parse_WhenCsvHasCompleteRow_ReturnsIsCompleteTrue()
    {
        var csv = BuildCsvHeader() + "\n" + BuildCompleteRow();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var error = _service.Parse(stream, "test.csv", out var rows, out var skipped);

        Assert.Null(error);
        Assert.Equal(0, skipped);
        Assert.Single(rows);
        Assert.True(rows[0].IsComplete);
        Assert.Equal("REF1", rows[0].Request.ReferenceNumber);
        Assert.Equal("PS1", rows[0].Request.PostalService);
        Assert.Equal("Receiver Co", rows[0].Request.ReceiverName);
        Assert.Equal("Shipper Co", rows[0].Request.Shipper?.Name);
    }

    [Fact]
    public void Parse_WhenRowMissingRequiredFields_ReturnsIsCompleteFalse()
    {
        var csv = BuildCsvHeader() + "\n" + BuildDraftRow();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var error = _service.Parse(stream, "test.csv", out var rows, out var skipped);

        Assert.Null(error);
        Assert.Equal(0, skipped);
        Assert.Single(rows);
        Assert.False(rows[0].IsComplete);
        Assert.Equal("REF2", rows[0].Request.ReferenceNumber);
    }

    [Fact]
    public void Parse_WhenMultipleRows_ReturnsCorrectMix()
    {
        var csv = BuildCsvHeader() + "\n" + BuildCompleteRow() + "\n" + BuildDraftRow() + "\n" + BuildCompleteRow();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var error = _service.Parse(stream, "test.csv", out var rows, out var skipped);

        Assert.Null(error);
        Assert.Equal(0, skipped);
        Assert.Equal(3, rows.Count);
        Assert.True(rows[0].IsComplete);
        Assert.False(rows[1].IsComplete);
        Assert.True(rows[2].IsComplete);
    }

    private static string BuildCsvHeader()
    {
        return "Id,ShipmentNumber,WaybillNumber,CustomerName,CreatedAtUtc,Enabled,ReferenceNumber,PostalService," +
               "ShipperName,ShipperAddress,ShipperCity,ShipperPostalCode,ShipperCountry,ShipperEmail,ShipperPhone," +
               "ReceiverName,ReceiverAddress,ReceiverCity,ReceiverPostalCode,ReceiverCountry,ReceiverEmail,ReceiverPhone," +
               "Service,SenderReference,ReceiverReference,FreightPayer,GrossWeight,GrossVolume,PackageQuantity," +
               "PackageWeight,PackageVolume,PackageType,PackageDescription,PackageDimensions";
    }

    private static string BuildCompleteRow()
    {
        return ",,,,2024-01-01T00:00:00Z,Yes,REF1,PS1," +
               "Shipper Co,Addr 1,Helsinki,00100,FI,,," +
               "Receiver Co,Addr 2,Espoo,02100,FI,,," +
               "Express,,,FP,5,0.1,1,2,,Box,,,";
    }

    private static string BuildDraftRow()
    {
        return ",,,,2024-01-01T00:00:00Z,Yes,REF2,PS1," +
               "Shipper Co,Addr 1,Helsinki,00100,FI,,," +
               "Receiver Co,,Espoo,02100,FI,,," +  // Missing ReceiverAddress
               "Express,,,FP,5,0.1,1,,,Box,,,";   // Missing PackageWeight
    }
}
