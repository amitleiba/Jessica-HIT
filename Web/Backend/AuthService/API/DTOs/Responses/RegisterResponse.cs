using System.ComponentModel.DataAnnotations;

namespace AuthService.API.DTOs.Responses;

/// <summary>
/// Response DTO for user registration.
/// Matches the frontend's existing RegisterResponse contract.
/// </summary>
public class RegisterResponse
{
    [Required]
    public required string Message { get; init; }

    public string? UserId { get; init; }
}