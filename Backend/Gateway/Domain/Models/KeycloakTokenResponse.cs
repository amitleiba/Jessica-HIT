using System.Text.Json.Serialization;

namespace Gateway.Domain.Models;

/// <summary>
/// Response model for Keycloak token endpoint (OAuth2 token response)
/// Represents the JSON response from Keycloak's /protocol/openid-connect/token endpoint
/// </summary>
public class KeycloakTokenResponse
{
    /// <summary>
    /// The access token (JWT) issued by Keycloak
    /// </summary>
    [JsonPropertyName("access_token")]
    public string? AccessToken { get; set; }

    /// <summary>
    /// The type of token, typically "Bearer"
    /// </summary>
    [JsonPropertyName("token_type")]
    public string? TokenType { get; set; }

    /// <summary>
    /// The lifetime in seconds of the access token
    /// </summary>
    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    /// <summary>
    /// The refresh token used to obtain new access tokens
    /// </summary>
    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }
}
