using System.ComponentModel.DataAnnotations;

namespace AuthService.API.DTOs.Requests;

/// <summary>
/// Request DTO for refreshing an access token.
/// The expired access token is used to identify the user (sub claim decoded without validation).
/// The refresh token is verified against stored hashes.
/// </summary>
public class RefreshTokenRequest
{
    [Required(ErrorMessage = "Refresh token is required")]
    public required string RefreshToken { get; init; }

    /// <summary>
    /// The expired access token. Used to extract the user's ID (sub claim)
    /// so we only check refresh tokens for that specific user.
    /// </summary>
    [Required(ErrorMessage = "Access token is required")]
    public required string AccessToken { get; init; }
}