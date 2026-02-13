using System.ComponentModel.DataAnnotations;

namespace AuthService.Domain.Configuration;

/// <summary>
/// JWT token configuration.
/// Bound from appsettings.json "Jwt" section.
/// The SecretKey MUST be shared with the Gateway for token validation.
/// </summary>
public class JwtConfig
{
    public const string SectionName = "Jwt";

    /// <summary>
    /// HMAC-SHA256 signing key. Must be at least 32 characters.
    /// Shared between AuthService (signs tokens) and Gateway (validates tokens).
    /// </summary>
    [Required(ErrorMessage = "JWT SecretKey is required")]
    [MinLength(32, ErrorMessage = "JWT SecretKey must be at least 32 characters")]
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Token issuer claim (iss). E.g. "jessica-auth"
    /// </summary>
    [Required(ErrorMessage = "JWT Issuer is required")]
    public string Issuer { get; set; } = "jessica-auth";

    /// <summary>
    /// Token audience claim (aud). E.g. "jessica-api"
    /// </summary>
    [Required(ErrorMessage = "JWT Audience is required")]
    public string Audience { get; set; } = "jessica-api";

    /// <summary>
    /// Access token expiry in minutes. Default: 15
    /// </summary>
    public int AccessTokenExpiryMinutes { get; set; } = 15;

    /// <summary>
    /// Refresh token expiry in days. Default: 7
    /// </summary>
    public int RefreshTokenExpiryDays { get; set; } = 7;
}

