using System.Text.Json.Serialization;

namespace CargoHub.Application.Bookings.Dtos;

/// <summary>Response from POST import/preview (counts + session id for confirm).</summary>
public sealed record ImportPreviewResponseDto(
    Guid SessionId,
    int CompletedCount,
    int DraftCount,
    int SkippedEmptyRows,
    int TotalDataRows);

/// <summary>Response from POST import/analyze (exact headers → same counts as preview; else mapping metadata).</summary>
public sealed class ImportAnalyzeResponseDto
{
    public bool NeedsMapping { get; init; }
    public Guid SessionId { get; init; }
    public int? CompletedCount { get; init; }
    public int? DraftCount { get; init; }
    public int? SkippedEmptyRows { get; init; }
    public int? TotalDataRows { get; init; }
    public IReadOnlyList<string>? FileHeaders { get; init; }
    public IReadOnlyList<string>? BookingFields { get; init; }
}

/// <summary>Raw table held in server cache until the user applies column mapping.</summary>
public sealed class ImportRawSessionState
{
    public required List<Dictionary<string, string?>> Rows { get; init; }
    public required List<string> FileHeaders { get; init; }
    public int SkippedEmptyRows { get; init; }
}

/// <summary>Body for POST import/apply-mapping (JSON camelCase).</summary>
public sealed class ImportApplyMappingRequestDto
{
    public Guid SessionId { get; set; }
    public Dictionary<string, string?>? ColumnMap { get; set; }
}

/// <summary>Body for POST import/confirm (JSON camelCase).</summary>
public sealed class ImportConfirmRequestDto
{
    public Guid SessionId { get; set; }
    public bool ImportCompleted { get; set; }
    public bool ImportDrafts { get; set; }
}

/// <summary>Body for POST waybills/bulk (JSON camelCase bookingIds).</summary>
public sealed class BulkWaybillRequestDto
{
    [JsonPropertyName("bookingIds")]
    public List<Guid>? BookingIds { get; set; }
}
