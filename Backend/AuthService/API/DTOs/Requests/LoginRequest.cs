using System.ComponentModel.DataAnnotations;

namespace AuthService.API.DTOs.Requests;

/// <summary>
/// Request DTO for user login.
/// Matches the frontend's existing LoginRequest contract.
/// </summary>
public class LoginRequest
{
    [Required(ErrorMessage = "Username is required")]
    public required string Username { get; init; }

    [Required(ErrorMessage = "Password is required")]
    public required string Password { get; init; }
}

