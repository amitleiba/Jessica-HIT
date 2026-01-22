using System.ComponentModel.DataAnnotations;

namespace Gateway.API.DTOs.Requests;

/// <summary>
/// Request DTO for user login
/// </summary>
public class LoginRequest
{
    [Required(ErrorMessage = "Username is required")]
    public required string Username { get; init; }

    [Required(ErrorMessage = "Password is required")]
    public required string Password { get; init; }
}
