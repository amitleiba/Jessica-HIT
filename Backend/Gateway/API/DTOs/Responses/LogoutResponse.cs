using System.ComponentModel.DataAnnotations;

namespace Gateway.API.DTOs.Responses;

/// <summary>
/// Response DTO for the logout endpoint
/// </summary>
public class LogoutResponse
{
    /// <summary>
    /// Message describing the logout result
    /// </summary>
    [Required]
    public required string Message { get; init; }

    /// <summary>
    /// Username of the logged out user (optional, for JWT Bearer logout)
    /// </summary>
    public string? Username { get; init; }
}
