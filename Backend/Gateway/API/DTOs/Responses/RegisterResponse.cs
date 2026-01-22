using System.ComponentModel.DataAnnotations;

namespace Gateway.API.DTOs.Responses;

/// <summary>
/// Response DTO for user registration
/// </summary>
public class RegisterResponse
{
    [Required]
    public required string Message { get; init; }
    
    public string? UserId { get; init; }
}
