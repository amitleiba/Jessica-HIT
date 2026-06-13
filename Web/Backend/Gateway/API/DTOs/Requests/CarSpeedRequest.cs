using System.ComponentModel.DataAnnotations;

namespace Gateway.API.DTOs.Requests;

/// <summary>
/// Request DTO for car speed change events received via SignalR.
/// Speed is an integer value between 0 (min) and 100 (max).
/// </summary>
public class CarSpeedRequest
{
    [Required(ErrorMessage = "Speed is required")]
    [Range(0, 100, ErrorMessage = "Speed must be between 0 and 100")]
    public required int Speed { get; init; }
}

