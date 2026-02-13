using System.ComponentModel.DataAnnotations;

namespace AuthService.API.DTOs.Responses;

/// <summary>
/// Response DTO for logout endpoint.
/// Matches the frontend's existing LogoutResponse contract.
/// </summary>
public class LogoutResponse
{
    [Required]
    public required string Message { get; init; }

    public string? Username { get; init; }
}

