namespace CargoHub.Application.Bookings;

/// <summary>Persisted booking import column maps per company and file layout.</summary>
public interface IImportFileMappingRepository
{
    Task<IReadOnlyDictionary<string, string?>?> GetColumnMapAsync(
        Guid companyId,
        string fileNameKey,
        string headerSignature,
        CancellationToken cancellationToken = default);

    Task UpsertAsync(
        Guid companyId,
        string fileNameKey,
        string headerSignature,
        IReadOnlyDictionary<string, string?> columnMap,
        CancellationToken cancellationToken = default);
}
