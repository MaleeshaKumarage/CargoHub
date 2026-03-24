using System.Globalization;
using System.Text;
using CargoHub.Application.Bookings.Dtos;
using CsvHelper;
using CsvHelper.Configuration;
using ClosedXML.Excel;

namespace CargoHub.Api.Services;

/// <summary>
/// Imports bookings from CSV or Excel. Exact template match (after trim + BOM) skips column mapping.
/// </summary>
public sealed class BookingImportService
{
    /// <summary>Expected column names (case-sensitive). Must match export format when unmapped.</summary>
    private static readonly string[] ExpectedHeaders =
    {
        "CreatedAtUtc", "CustomerName", "Enabled", "FreightPayer", "GrossVolume", "GrossWeight", "Id",
        "PackageDescription", "PackageDimensions", "PackageQuantity", "PackageType", "PackageVolume", "PackageWeight",
        "PostalService", "ReceiverAddress", "ReceiverCity", "ReceiverCountry", "ReceiverEmail", "ReceiverName",
        "ReceiverPhone", "ReceiverPostalCode", "ReceiverReference", "ReferenceNumber", "SenderReference", "Service",
        "ShipperAddress", "ShipperCity", "ShipperCountry", "ShipperEmail", "ShipperName", "ShipperPhone",
        "ShipperPostalCode", "ShipmentNumber", "WaybillNumber",
    };

    /// <summary>Canonical booking/import field names (same order as export).</summary>
    public static IReadOnlyList<string> BookingImportFieldNames => ExpectedHeaders;

    private static readonly HashSet<string> ExpectedSet = new(ExpectedHeaders, StringComparer.Ordinal);

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
    /// Single pass: exact header match → parsed rows; otherwise raw rows + disambiguated file headers for mapping UI.
    /// </summary>
    public ImportAnalysisResult AnalyzeImport(Stream stream, string fileName)
    {
        var isExcel = fileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) ||
                      fileName.EndsWith(".xls", StringComparison.OrdinalIgnoreCase);
        return isExcel ? AnalyzeExcel(stream) : AnalyzeCsv(stream);
    }

    /// <summary>
    /// Build <see cref="ImportRow"/> list from raw file rows using a map: canonical field → disambiguated file column (or null).
    /// </summary>
    public string? ParseFromMappedRows(
        IReadOnlyList<Dictionary<string, string?>> rawRows,
        IReadOnlyDictionary<string, string?> columnMap,
        IReadOnlyList<string> allowedFileHeaders,
        out List<ImportRow> rows)
    {
        rows = new List<ImportRow>();
        var allowed = new HashSet<string>(allowedFileHeaders, StringComparer.Ordinal);
        foreach (var (canonical, fileCol) in columnMap)
        {
            if (!ExpectedSet.Contains(canonical))
                return $"Unknown booking field: '{canonical}'.";
            if (!string.IsNullOrEmpty(fileCol) && !allowed.Contains(fileCol!))
                return $"Mapped column '{fileCol}' is not in this file.";
        }

        foreach (var raw in rawRows)
        {
            var canonRow = new Dictionary<string, string?>(StringComparer.Ordinal);
            foreach (var h in ExpectedHeaders)
            {
                if (!columnMap.TryGetValue(h, out var fileColumn) || string.IsNullOrEmpty(fileColumn))
                {
                    canonRow[h] = null;
                    continue;
                }

                canonRow[h] = raw.TryGetValue(fileColumn!, out var v) ? v : null;
            }

            var (request, isComplete) = MapToRequest(canonRow);
            rows.Add(new ImportRow(request, isComplete));
        }

        return null;
    }

    /// <summary>Parse when file headers match export (after normalization).</summary>
    public string? Parse(Stream stream, string fileName, out List<ImportRow> rows, out int skippedEmptyRows)
    {
        var analysis = AnalyzeImport(stream, fileName);
        if (analysis.Error != null)
        {
            rows = new List<ImportRow>();
            skippedEmptyRows = 0;
            return analysis.Error;
        }

        if (analysis.NeedsMapping)
        {
            rows = new List<ImportRow>();
            skippedEmptyRows = 0;
            return "File columns do not match the export template. Use column mapping in the portal or fix headers.";
        }

        rows = analysis.ParsedRows!;
        skippedEmptyRows = analysis.SkippedEmptyRows;
        return null;
    }

    private ImportAnalysisResult AnalyzeCsv(Stream stream)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            TrimOptions = TrimOptions.Trim,
            MissingFieldFound = null,
            BadDataFound = null,
        };
        using var csv = new CsvReader(reader, config);
        if (!csv.Read())
            return ImportAnalysisResult.Fail("File has no data.");
        csv.ReadHeader();
        var rawHeaders = csv.HeaderRecord;
        if (rawHeaders == null || rawHeaders.Length == 0)
            return ImportAnalysisResult.Fail("File has no header row.");

        var normalized = NormalizeHeaderRow(rawHeaders);
        var disambiguated = BuildDisambiguatedLabels(normalized);
        var headerIndex = BuildHeaderIndex(disambiguated);
        var exact = HeadersMatchExport(normalized);

        var parsedRows = new List<ImportRow>();
        var rawRows = new List<Dictionary<string, string?>>();
        var skipped = 0;
        while (csv.Read())
        {
            var row = ReadCsvRow(csv, headerIndex);
            if (row == null)
            {
                skipped++;
                continue;
            }

            if (exact)
            {
                var (req, complete) = MapToRequest(row);
                parsedRows.Add(new ImportRow(req, complete));
            }
            else
                rawRows.Add(row);
        }

        return exact
            ? ImportAnalysisResult.Parsed(parsedRows, skipped)
            : ImportAnalysisResult.Raw(disambiguated.ToList(), rawRows, skipped);
    }

    private static ImportAnalysisResult AnalyzeExcel(Stream stream)
    {
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheets.FirstOrDefault(w => w.Name == "Bookings") ?? workbook.Worksheet(1);
        var firstRow = worksheet.FirstRowUsed();
        if (firstRow == null)
            return ImportAnalysisResult.Fail("File has no data.");

        var rawList = new List<string>();
        var col = 1;
        while (true)
        {
            var cell = worksheet.Cell(firstRow.RowNumber(), col);
            var val = cell.GetString().Trim();
            if (string.IsNullOrEmpty(val)) break;
            rawList.Add(val);
            col++;
        }

        if (rawList.Count == 0)
            return ImportAnalysisResult.Fail("File has no header row.");

        var normalized = NormalizeHeaderRow(rawList.ToArray());
        var disambiguated = BuildDisambiguatedLabels(normalized);
        var headerIndex = BuildHeaderIndex(disambiguated);
        var dataStartRow = firstRow.RowNumber() + 1;
        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? dataStartRow - 1;

        if (HeadersMatchExport(normalized))
        {
            var rows = new List<ImportRow>();
            var skipped = 0;
            for (var r = dataStartRow; r <= lastRow; r++)
            {
                var row = ReadExcelRow(worksheet, r, headerIndex);
                if (row == null)
                {
                    skipped++;
                    continue;
                }

                var (req, complete) = MapToRequest(row);
                rows.Add(new ImportRow(req, complete));
            }

            return ImportAnalysisResult.Parsed(rows, skipped);
        }

        {
            var rawRows = new List<Dictionary<string, string?>>();
            var skipped = 0;
            for (var r = dataStartRow; r <= lastRow; r++)
            {
                var row = ReadExcelRow(worksheet, r, headerIndex);
                if (row == null)
                {
                    skipped++;
                    continue;
                }

                rawRows.Add(row);
            }

            return ImportAnalysisResult.Raw(disambiguated.ToList(), rawRows, skipped);
        }
    }

    private static string[] NormalizeHeaderRow(string[] headers)
    {
        var result = new string[headers.Length];
        for (var i = 0; i < headers.Length; i++)
        {
            var s = headers[i]?.Trim() ?? "";
            if (i == 0)
                s = s.TrimStart('\uFEFF');
            result[i] = s;
        }

        return result;
    }

    private static bool HeadersMatchExport(string[] normalizedHeaders)
    {
        if (normalizedHeaders.Length != ExpectedHeaders.Length)
            return false;
        var actual = new HashSet<string>(normalizedHeaders, StringComparer.Ordinal);
        if (actual.Count != ExpectedHeaders.Length)
            return false;
        foreach (var h in ExpectedHeaders)
        {
            if (!actual.Contains(h))
                return false;
        }

        return true;
    }

    /// <summary>First occurrence keeps base name; duplicates become "Name (2)", "Name (3)", …</summary>
    private static string[] BuildDisambiguatedLabels(string[] normalizedHeaders)
    {
        var seen = new Dictionary<string, int>(StringComparer.Ordinal);
        var result = new string[normalizedHeaders.Length];
        for (var i = 0; i < normalizedHeaders.Length; i++)
        {
            var h = normalizedHeaders[i];
            if (!seen.TryGetValue(h, out var n))
            {
                seen[h] = 1;
                result[i] = h;
            }
            else
            {
                n++;
                seen[h] = n;
                result[i] = $"{h} ({n})";
            }
        }

        return result;
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

/// <summary>Outcome of <see cref="BookingImportService.AnalyzeImport"/>.</summary>
public sealed class ImportAnalysisResult
{
    public string? Error { get; private init; }
    public bool NeedsMapping { get; private init; }
    public List<BookingImportService.ImportRow>? ParsedRows { get; private init; }
    public List<Dictionary<string, string?>>? RawRows { get; private init; }
    public List<string>? FileHeaders { get; private init; }
    public int SkippedEmptyRows { get; private init; }

    public static ImportAnalysisResult Fail(string message) => new() { Error = message };

    public static ImportAnalysisResult Parsed(List<BookingImportService.ImportRow> rows, int skipped) => new()
    {
        NeedsMapping = false,
        ParsedRows = rows,
        SkippedEmptyRows = skipped,
    };

    public static ImportAnalysisResult Raw(List<string> fileHeaders, List<Dictionary<string, string?>> rawRows, int skipped) => new()
    {
        NeedsMapping = true,
        FileHeaders = fileHeaders,
        RawRows = rawRows,
        SkippedEmptyRows = skipped,
    };
}
