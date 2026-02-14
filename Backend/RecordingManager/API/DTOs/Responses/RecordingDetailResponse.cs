namespace RecordingManager.API.DTOs.Responses;

/// <summary>
/// Full recording with event timeline — used for replay.
/// </summary>
public class RecordingDetailResponse
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required int Speed { get; init; }
    public required long DurationMs { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required IReadOnlyList<RecordingEventResponse> Events { get; init; }
}

/// <summary>
/// A single event in the recording timeline.
/// </summary>
public class RecordingEventResponse
{
    public required long OffsetMs { get; init; }
    public required string Direction { get; init; }
}

