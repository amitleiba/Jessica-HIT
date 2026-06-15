using System.ComponentModel.DataAnnotations;

namespace Gateway.API.DTOs.Requests;

/// <summary>
/// Request DTO for car direction change events received via SignalR.
/// Direction values: "up", "down", "left", "right", or combos like "left-right", "down-up", etc.
/// </summary>
public class CarDirectionRequest
{
    [Required(ErrorMessage = "Direction is required")]
    public required string Direction { get; init; }

    [Range(0, 100, ErrorMessage = "Speed must be between 0 and 100")]
    public required int Speed { get; init; }
}

