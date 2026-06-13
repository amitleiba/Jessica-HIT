namespace RecordingManager.API.DTOs.Responses;

/// <summary>
/// Lightweight recording info for list views (no events payload).
/// </summary>
public class RecordingSummaryResponse
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required int Speed { get; init; }
    public required long DurationMs { get; init; }
    public required DateTime CreatedAt { get; init; }
}

