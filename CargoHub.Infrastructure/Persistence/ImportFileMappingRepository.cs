using System.Text.Json;
using CargoHub.Application.Bookings;
using CargoHub.Domain.Companies;
using Microsoft.EntityFrameworkCore;

namespace CargoHub.Infrastructure.Persistence;

public sealed class ImportFileMappingRepository : IImportFileMappingRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false,
    };

    private readonly ApplicationDbContext _db;

    public ImportFileMappingRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyDictionary<string, string?>?> GetColumnMapAsync(
        Guid companyId,
        string fileNameKey,
        string headerSignature,
        CancellationToken cancellationToken = default)
    {
        var row = await _db.BookingImportFileMappings.AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.CompanyId == companyId && x.FileNameKey == fileNameKey && x.HeaderSignature == headerSignature,
                cancellationToken);
        if (row == null || string.IsNullOrWhiteSpace(row.ColumnMapJson))
            return null;
        var map = JsonSerializer.Deserialize<Dictionary<string, string?>>(row.ColumnMapJson, JsonOptions);
        return map;
    }

    public async Task UpsertAsync(
        Guid companyId,
        string fileNameKey,
        string headerSignature,
        IReadOnlyDictionary<string, string?> columnMap,
        CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(columnMap, JsonOptions);
        var utc = DateTime.UtcNow;
        var existing = await _db.BookingImportFileMappings
            .FirstOrDefaultAsync(
                x => x.CompanyId == companyId && x.FileNameKey == fileNameKey && x.HeaderSignature == headerSignature,
                cancellationToken);
        if (existing == null)
        {
            _db.BookingImportFileMappings.Add(new BookingImportFileMapping
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                FileNameKey = fileNameKey,
                HeaderSignature = headerSignature,
                ColumnMapJson = json,
                CreatedAtUtc = utc,
                UpdatedAtUtc = utc,
            });
        }
        else
        {
            existing.ColumnMapJson = json;
            existing.UpdatedAtUtc = utc;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }
}
