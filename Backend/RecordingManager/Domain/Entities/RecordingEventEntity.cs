namespace RecordingManager.Domain.Entities;

/// <summary>
/// A single direction-change event captured during a recording session.
/// Stored with millisecond-precision offset from the recording start.
/// </summary>
public class RecordingEventEntity
{
    public Guid Id { get; set; }

    /// <summary>
    /// FK to the parent recording.
    /// </summary>
    public Guid RecordingId { get; set; }

    /// <summary>
    /// Milliseconds elapsed since the recording started.
    /// </summary>
    public long OffsetMs { get; set; }

    /// <summary>
    /// Direction string: "up", "down", "left", "right", "left-right", "idle", etc.
    /// </summary>
    public required string Direction { get; set; }

    // ── Navigation ──
    public RecordingEntity Recording { get; set; } = null!;
}

