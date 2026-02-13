using System.ComponentModel.DataAnnotations;

namespace Gateway.Domain.Configuration;

/// <summary>
/// JWT validation configuration for the Gateway.
/// Must match the AuthService's JWT signing configuration.
/// Bound from appsettings.json "Jwt" section.
/// </summary>
public class JwtConfig
{
    public const string SectionName = "Jwt";

    /// <summary>
    /// HMAC-SHA256 signing key. Must match the AuthService's SecretKey exactly.
    /// </summary>
    [Required(ErrorMessage = "JWT SecretKey is required")]
    [MinLength(32, ErrorMessage = "JWT SecretKey must be at least 32 characters")]
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Expected token issuer. Must match AuthService's Issuer.
    /// </summary>
    [Required(ErrorMessage = "JWT Issuer is required")]
    public string Issuer { get; set; } = "jessica-auth";

    /// <summary>
    /// Expected token audience. Must match AuthService's Audience.
    /// </summary>
    [Required(ErrorMessage = "JWT Audience is required")]
    public string Audience { get; set; } = "jessica-api";
}

