using System.ComponentModel.DataAnnotations;

namespace Gateway.API.DTOs.Responses;

/// <summary>
/// Response DTO for successful login
/// Contains JWT access token and user information
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
