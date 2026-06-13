using System.ComponentModel.DataAnnotations;

namespace RecordingManager.API.DTOs.Requests;

/// <summary>
/// Request body for creating a new recording.
/// Sent by the frontend after the user stops recording.
/// </summary>
public class CreateRecordingRequest
{
    [Required(ErrorMessage = "Recording name is required")]
    [MaxLength(200, ErrorMessage = "Recording name cannot exceed 200 characters")]
    public required string Name { get; init; }

    [Required(ErrorMessage = "Speed is required")]
    [Range(0, 100, ErrorMessage = "Speed must be between 0 and 100")]
    public required int Speed { get; init; }

    [Required(ErrorMessage = "Duration is required")]
    [Range(0, long.MaxValue, ErrorMessage = "Duration must be non-negative")]
    public required long DurationMs { get; init; }

    [Required(ErrorMessage = "Events are required")]
    public required IReadOnlyList<RecordingEventDto> Events { get; init; }
}

/// <summary>
/// A single direction-change event within a recording.
/// </summary>
public class RecordingEventDto
{
    [Required]
    [Range(0, long.MaxValue)]
    public required long OffsetMs { get; init; }

    [Required]
    [MaxLength(50)]
    public required string Direction { get; init; }
}

