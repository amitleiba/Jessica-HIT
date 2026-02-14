using System.ComponentModel.DataAnnotations;

namespace AuthService.API.DTOs.Responses;

/// <summary>
/// Response DTO for successful login and token refresh.
/// Matches the frontend's existing BackendLoginResponse contract exactly.
/// </summary>
public class LoginResponse
{
    [Required]
    public required string AccessToken { get; init; }

    [Required]
    public required string TokenType { get; init; } = "Bearer";

    [Required]
    public required int ExpiresIn { get; init; }

    public string? RefreshToken { get; init; }

    [Required]
    public required UserInfoResponse UserInfo { get; init; }
}