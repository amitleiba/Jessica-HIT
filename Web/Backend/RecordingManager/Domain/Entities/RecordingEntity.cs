namespace RecordingManager.Domain.Entities;

/// <summary>
/// A saved driving-session recording.
/// Each recording belongs to a single user and contains a timeline of direction events.
/// Speed is fixed for the entire recording (set once before the session starts).
/// </summary>
public class RecordingEntity
{
    public Guid Id { get; set; }

    /// <summary>
    /// Owner — the authenticated user's unique ID (JWT "sub" claim).
    /// Forwarded by the Gateway as X-User-Id header.
    /// </summary>
    public required string UserId { get; set; }

    /// <summary>
    /// User-given display name for this recording.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Fixed speed for the entire recording (0–100).
    /// </summary>
    public int Speed { get; set; }

    /// <summary>
    /// Total recording duration in milliseconds.
    /// </summary>
    public long DurationMs { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // ── Navigation ──
#pragma warning disable CA2227 // EF Core requires collection navigation properties to have setters
    public ICollection<RecordingEventEntity> Events { get; set; } = [];
#pragma warning restore CA2227
}

