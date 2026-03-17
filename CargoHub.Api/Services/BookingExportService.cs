using System.Globalization;
using System.Text;
using CargoHub.Application.Bookings.Dtos;
using CsvHelper;
using CsvHelper.Configuration;
using ClosedXML.Excel;

namespace CargoHub.Api.Services;

/// <summary>
/// Exports bookings to CSV or Excel format.
/// </summary>
public sealed class BookingExportService
{
    /// <summary>
    /// Export a single booking to CSV bytes.
    /// </summary>
    public byte[] ExportSingleToCsv(BookingDetailDto booking)
    {
        var rows = FlattenSingle(booking);
        return WriteCsv(rows);
    }

    /// <summary>
    /// Export multiple bookings to CSV bytes.
    /// </summary>
    public byte[] ExportBulkToCsv(IEnumerable<BookingDetailDto> bookings)
    {
        var rows = bookings.SelectMany(FlattenSingle).ToList();
        return WriteCsv(rows);
    }

    /// <summary>
    /// Export a single booking to Excel bytes.
    /// </summary>
    public byte[] ExportSingleToExcel(BookingDetailDto booking)
    {
        var rows = FlattenSingle(booking);
        return WriteExcel(rows);
    }

    /// <summary>
    /// Export multiple bookings to Excel bytes.
    /// </summary>
    public byte[] ExportBulkToExcel(IEnumerable<BookingDetailDto> bookings)
    {
        var rows = bookings.SelectMany(FlattenSingle).ToList();
        return WriteExcel(rows);
    }

    private static List<BookingExportRow> FlattenSingle(BookingDetailDto b)
    {
        var list = new List<BookingExportRow>();
        if (b.Packages != null && b.Packages.Count > 0)
        {
            foreach (var pkg in b.Packages)
            {
                list.Add(CreateRow(b, pkg));
            }
        }
        else
        {
            list.Add(CreateRow(b, null));
        }
        return list;
    }

    private static BookingExportRow CreateRow(BookingDetailDto b, BookingPackageDto? pkg)
    {
        return new BookingExportRow
        {
            Id = b.Id.ToString("N"),
            ShipmentNumber = b.ShipmentNumber,
            WaybillNumber = b.WaybillNumber,
            CustomerName = b.CustomerName,
            CreatedAtUtc = b.CreatedAtUtc.ToString("o"),
            Enabled = b.Enabled ? "Yes" : "No",
            ReferenceNumber = b.Header?.ReferenceNumber,
            PostalService = b.Header?.PostalService,
            ShipperName = b.Shipper?.Name,
            ShipperAddress = b.Shipper != null ? $"{b.Shipper.Address1} {b.Shipper.Address2}".Trim() : null,
            ShipperCity = b.Shipper?.City,
            ShipperPostalCode = b.Shipper?.PostalCode,
            ShipperCountry = b.Shipper?.Country,
            ShipperEmail = b.Shipper?.Email,
            ShipperPhone = b.Shipper?.PhoneNumber ?? b.Shipper?.PhoneNumberMobile,
            ReceiverName = b.Receiver?.Name,
            ReceiverAddress = b.Receiver != null ? $"{b.Receiver.Address1} {b.Receiver.Address2}".Trim() : null,
            ReceiverCity = b.Receiver?.City,
            ReceiverPostalCode = b.Receiver?.PostalCode,
            ReceiverCountry = b.Receiver?.Country,
            ReceiverEmail = b.Receiver?.Email,
            ReceiverPhone = b.Receiver?.PhoneNumber ?? b.Receiver?.PhoneNumberMobile,
            Service = b.Shipment?.Service,
            SenderReference = b.Shipment?.SenderReference,
            ReceiverReference = b.Shipment?.ReceiverReference,
            FreightPayer = b.Shipment?.FreightPayer,
            GrossWeight = b.ShippingInfo?.GrossWeight,
            GrossVolume = b.ShippingInfo?.GrossVolume,
            PackageQuantity = b.ShippingInfo?.PackageQuantity,
            PackageWeight = pkg?.Weight,
            PackageVolume = pkg?.Volume,
            PackageType = pkg?.PackageType,
            PackageDescription = pkg?.Description,
            PackageDimensions = pkg != null && (pkg.Length != null || pkg.Width != null || pkg.Height != null)
                ? $"{pkg.Length ?? "—"} × {pkg.Width ?? "—"} × {pkg.Height ?? "—"}"
                : null,
        };
    }

    private static byte[] WriteCsv(List<BookingExportRow> rows)
    {
        using var ms = new MemoryStream();
        using var writer = new StreamWriter(ms, Encoding.UTF8);
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            TrimOptions = TrimOptions.Trim,
        };
        using var csv = new CsvWriter(writer, config);
        csv.WriteRecords(rows);
        writer.Flush();
        return ms.ToArray();
    }

    private static byte[] WriteExcel(List<BookingExportRow> rows)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Bookings");

        // Header row
        var props = typeof(BookingExportRow).GetProperties();
        for (var c = 0; c < props.Length; c++)
        {
            var name = props[c].Name;
            worksheet.Cell(1, c + 1).Value = name;
        }
        var headerRange = worksheet.Range(1, 1, 1, props.Length);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

        // Data rows
        var rowIndex = 2;
        foreach (var row in rows)
        {
            for (var c = 0; c < props.Length; c++)
            {
                var val = props[c].GetValue(row);
                worksheet.Cell(rowIndex, c + 1).Value = val?.ToString() ?? "";
            }
            rowIndex++;
        }

        worksheet.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms, false);
        return ms.ToArray();
    }

    internal sealed class BookingExportRow
    {
        public string Id { get; set; } = "";
        public string? ShipmentNumber { get; set; }
        public string? WaybillNumber { get; set; }
        public string? CustomerName { get; set; }
        public string CreatedAtUtc { get; set; } = "";
        public string Enabled { get; set; } = "";
        public string? ReferenceNumber { get; set; }
        public string? PostalService { get; set; }
        public string? ShipperName { get; set; }
        public string? ShipperAddress { get; set; }
        public string? ShipperCity { get; set; }
        public string? ShipperPostalCode { get; set; }
        public string? ShipperCountry { get; set; }
        public string? ShipperEmail { get; set; }
        public string? ShipperPhone { get; set; }
        public string? ReceiverName { get; set; }
        public string? ReceiverAddress { get; set; }
        public string? ReceiverCity { get; set; }
        public string? ReceiverPostalCode { get; set; }
        public string? ReceiverCountry { get; set; }
        public string? ReceiverEmail { get; set; }
        public string? ReceiverPhone { get; set; }
        public string? Service { get; set; }
        public string? SenderReference { get; set; }
        public string? ReceiverReference { get; set; }
        public string? FreightPayer { get; set; }
        public string? GrossWeight { get; set; }
        public string? GrossVolume { get; set; }
        public string? PackageQuantity { get; set; }
        public string? PackageWeight { get; set; }
        public string? PackageVolume { get; set; }
        public string? PackageType { get; set; }
        public string? PackageDescription { get; set; }
        public string? PackageDimensions { get; set; }
    }
}
