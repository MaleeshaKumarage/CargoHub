using System.Text.Json.Serialization;

namespace CargoHub.Application.Bookings.Dtos;

/// <summary>Response from POST import/preview (counts + session id for confirm).</summary>
public sealed record ImportPreviewResponseDto(
    Guid SessionId,
    int CompletedCount,
    int DraftCount,
    int SkippedEmptyRows,
    int TotalDataRows);

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
