using System.ComponentModel.DataAnnotations;

namespace Gateway.API.DTOs.Responses;

/// <summary>
/// Registration response (Gateway BFF contract; aligns with AuthService / frontend).
/// </summary>
public class RegisterResponse
{
    [Required]
    public required string Message { get; init; }

    public string? UserId { get; init; }
}
