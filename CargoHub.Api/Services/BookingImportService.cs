using System.Globalization;
using System.Text;
using CargoHub.Application.Bookings.Dtos;
using CsvHelper;
using CsvHelper.Configuration;
using ClosedXML.Excel;

namespace CargoHub.Api.Services;

/// <summary>
/// Imports bookings from CSV or Excel. Headers must match export format exactly.
/// Rows with all required data → completed booking; any missing → draft.
/// </summary>
public sealed class BookingImportService
{
    /// <summary>Expected column names (case-sensitive). Must match export format.</summary>
    private static readonly string[] ExpectedHeaders =
    {
        "CreatedAtUtc", "CustomerName", "Enabled", "FreightPayer", "GrossVolume", "GrossWeight", "Id",
        "PackageDescription", "PackageDimensions", "PackageQuantity", "PackageType", "PackageVolume", "PackageWeight",
        "PostalService", "ReceiverAddress", "ReceiverCity", "ReceiverCountry", "ReceiverEmail", "ReceiverName",
        "ReceiverPhone", "ReceiverPostalCode", "ReceiverReference", "ReferenceNumber", "SenderReference", "Service",
        "ShipperAddress", "ShipperCity", "ShipperCountry", "ShipperEmail", "ShipperName", "ShipperPhone",
        "ShipperPostalCode", "ShipmentNumber", "WaybillNumber",
    };

    /// <summary>Fields required for a row to be imported as completed (not draft).</summary>
    private static readonly HashSet<string> RequiredForComplete = new(StringComparer.Ordinal)
    {
        "ReferenceNumber", "PostalService",
        "ReceiverName", "ReceiverAddress", "ReceiverCity", "ReceiverPostalCode", "ReceiverCountry",
        "ShipperName", "ShipperAddress", "ShipperCity", "ShipperPostalCode", "ShipperCountry",
        "Service", "FreightPayer",
        "GrossWeight", "PackageWeight", "PackageType",
    };

    /// <summary>
    /// Parse a file and return import rows. Validates headers match exactly.
    /// </summary>
    /// <param name="stream">File stream (CSV or Excel).</param>
    /// <param name="fileName">Original filename (used to detect format).</param>
    /// <param name="rows">Parsed rows with request and IsComplete flag.</param>
    /// <returns>Error message if validation fails; null on success.</returns>
    public string? Parse(Stream stream, string fileName, out List<ImportRow> rows)
    {
        rows = new List<ImportRow>();
        var isExcel = fileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) ||
                     fileName.EndsWith(".xls", StringComparison.OrdinalIgnoreCase);

        if (isExcel)
            return ParseExcel(stream, out rows);
        return ParseCsv(stream, out rows);
    }

    private string? ParseCsv(Stream stream, out List<ImportRow> rows)
    {
        rows = new List<ImportRow>();
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            TrimOptions = TrimOptions.Trim,
            MissingFieldFound = null,
            BadDataFound = null,
        };
        using var csv = new CsvReader(reader, config);
        csv.Read();
        csv.ReadHeader();
        var headers = csv.HeaderRecord;
        if (headers == null || headers.Length == 0)
            return "File has no header row.";
        var validationError = ValidateHeaders(headers);
        if (validationError != null)
            return validationError;
        var headerIndex = BuildHeaderIndex(headers);
        while (csv.Read())
        {
            var row = ReadCsvRow(csv, headerIndex);
            if (row == null) continue; // skip empty rows
            var (request, isComplete) = MapToRequest(row);
            rows.Add(new ImportRow(request, isComplete));
        }
        return null;
    }

    private string? ParseExcel(Stream stream, out List<ImportRow> rows)
    {
        rows = new List<ImportRow>();
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheets.FirstOrDefault(w => w.Name == "Bookings") ?? workbook.Worksheet(1);
        var firstRow = worksheet.FirstRowUsed();
        if (firstRow == null)
            return "File has no data.";
        var headers = new List<string>();
        var col = 1;
        while (true)
        {
            var cell = worksheet.Cell(firstRow.RowNumber(), col);
            var val = cell.GetString().Trim();
            if (string.IsNullOrEmpty(val)) break;
            headers.Add(val);
            col++;
        }
        if (headers.Count == 0)
            return "File has no header row.";
        var validationError = ValidateHeaders(headers.ToArray());
        if (validationError != null)
            return validationError;
        var headerIndex = BuildHeaderIndex(headers.ToArray());
        var dataStartRow = firstRow.RowNumber() + 1;
        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? dataStartRow - 1;
        for (var r = dataStartRow; r <= lastRow; r++)
        {
            var row = ReadExcelRow(worksheet, r, headerIndex);
            if (row == null) continue;
            var (request, isComplete) = MapToRequest(row);
            rows.Add(new ImportRow(request, isComplete));
        }
        return null;
    }

    private static string? ValidateHeaders(string[] headers)
    {
        var expected = new HashSet<string>(ExpectedHeaders, StringComparer.Ordinal);
        var actual = new HashSet<string>(headers, StringComparer.Ordinal);
        if (actual.Count != expected.Count)
            return $"Header column count mismatch. Expected {expected.Count} columns: {string.Join(", ", ExpectedHeaders)}.";
        foreach (var h in expected)
        {
            if (!actual.Contains(h))
                return $"Missing column: '{h}'. Headers must match export format exactly (case-sensitive).";
        }
        return null;
    }

    private static Dictionary<string, int> BuildHeaderIndex(string[] headers)
    {
        var index = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < headers.Length; i++)
            index[headers[i]] = i;
        return index;
    }

    private static Dictionary<string, string?>? ReadCsvRow(CsvReader csv, Dictionary<string, int> headerIndex)
    {
        var row = new Dictionary<string, string?>(StringComparer.Ordinal);
        var hasAny = false;
        foreach (var (name, idx) in headerIndex)
        {
            var val = csv.GetField(idx)?.Trim();
            row[name] = string.IsNullOrEmpty(val) ? null : val;
            if (!string.IsNullOrEmpty(val)) hasAny = true;
        }
        return hasAny ? row : null;
    }

    private static Dictionary<string, string?>? ReadExcelRow(IXLWorksheet worksheet, int rowNum, Dictionary<string, int> headerIndex)
    {
        var row = new Dictionary<string, string?>(StringComparer.Ordinal);
        var hasAny = false;
        foreach (var (name, colIdx) in headerIndex)
        {
            var val = worksheet.Cell(rowNum, colIdx + 1).GetString().Trim();
            row[name] = string.IsNullOrEmpty(val) ? null : val;
            if (!string.IsNullOrEmpty(val)) hasAny = true;
        }
        return hasAny ? row : null;
    }

    private static (CreateBookingRequest Request, bool IsComplete) MapToRequest(Dictionary<string, string?> row)
    {
        string? Get(string key) => row.TryGetValue(key, out var v) ? v : null;

        var refNum = Get("ReferenceNumber");
        var postalService = Get("PostalService");
        var receiverName = Get("ReceiverName");
        var receiverAddress = Get("ReceiverAddress");
        var receiverCity = Get("ReceiverCity");
        var receiverPostalCode = Get("ReceiverPostalCode");
        var receiverCountry = Get("ReceiverCountry");
        var shipperName = Get("ShipperName");
        var shipperAddress = Get("ShipperAddress");
        var shipperCity = Get("ShipperCity");
        var shipperPostalCode = Get("ShipperPostalCode");
        var shipperCountry = Get("ShipperCountry");
        var service = Get("Service");
        var freightPayer = Get("FreightPayer");
        var grossWeight = Get("GrossWeight");
        var packageWeight = Get("PackageWeight");
        var packageType = Get("PackageType");

        var isComplete = RequiredForComplete.All(key =>
        {
            var v = Get(key);
            return !string.IsNullOrWhiteSpace(v);
        });

        var (pkgWeight, pkgVol, pkgDesc, pkgDims) = ParsePackageFields(row);

        var request = new CreateBookingRequest
        {
            ReferenceNumber = refNum,
            PostalService = postalService,
            ReceiverName = receiverName,
            ReceiverAddress1 = receiverAddress,
            ReceiverCity = receiverCity,
            ReceiverPostalCode = receiverPostalCode,
            ReceiverCountry = receiverCountry ?? "FI",
            ReceiverEmail = Get("ReceiverEmail"),
            ReceiverPhone = Get("ReceiverPhone"),
            Shipper = new CreateBookingPartyDto
            {
                Name = shipperName,
                Address1 = shipperAddress,
                City = shipperCity,
                PostalCode = shipperPostalCode,
                Country = shipperCountry ?? "FI",
                Email = Get("ShipperEmail"),
                PhoneNumber = Get("ShipperPhone"),
            },
            Shipment = new CreateBookingShipmentDto
            {
                Service = service,
                SenderReference = Get("SenderReference"),
                ReceiverReference = Get("ReceiverReference"),
                FreightPayer = freightPayer,
            },
            ShippingInfo = new CreateBookingShippingInfoDto
            {
                GrossWeight = grossWeight ?? "0",
                GrossVolume = Get("GrossVolume") ?? "0",
                PackageQuantity = Get("PackageQuantity") ?? "1",
                Packages = new List<CreateBookingPackageDto>
                {
                    new()
                    {
                        Weight = pkgWeight ?? packageWeight,
                        Volume = pkgVol,
                        PackageType = packageType,
                        Description = pkgDesc,
                        Length = pkgDims.Length,
                        Width = pkgDims.Width,
                        Height = pkgDims.Height,
                    },
                },
            },
        };
        return (request, isComplete);
    }

    private static (string? Weight, string? Volume, string? Description, (string? Length, string? Width, string? Height)) ParsePackageFields(Dictionary<string, string?> row)
    {
        string? Get(string key) => row.TryGetValue(key, out var v) ? v : null;
        var dims = Get("PackageDimensions");
        string? len = null, wid = null, hgt = null;
        if (!string.IsNullOrWhiteSpace(dims))
        {
            var parts = dims.Split('×', 'x', 'X').Select(s => s.Trim()).ToArray();
            if (parts.Length >= 3)
            {
                len = parts[0] == "—" ? null : parts[0];
                wid = parts[1] == "—" ? null : parts[1];
                hgt = parts[2] == "—" ? null : parts[2];
            }
        }
        return (Get("PackageWeight"), Get("PackageVolume"), Get("PackageDescription"), (len, wid, hgt));
    }

    public sealed record ImportRow(CreateBookingRequest Request, bool IsComplete);
}
