using System.ComponentModel.DataAnnotations;

namespace Gateway.API.DTOs.Responses;

/// <summary>
/// Login / refresh response (Gateway BFF contract; aligns with AuthService / frontend).
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
