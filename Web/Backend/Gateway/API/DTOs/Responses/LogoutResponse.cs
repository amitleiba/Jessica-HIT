using System.ComponentModel.DataAnnotations;

namespace Gateway.API.DTOs.Responses;

/// <summary>
/// Logout response (Gateway BFF contract; aligns with AuthService / frontend).
/// </summary>
public class LogoutResponse
{
    [Required]
    public required string Message { get; init; }

    public string? Username { get; init; }
}
