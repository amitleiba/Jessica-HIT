using System.Security.Claims;
using Gateway.Domain.Models;

namespace Gateway.Infrastructure.Adapters;

/// <summary>
/// Infrastructure interface for Keycloak operations
/// Implemented by Infrastructure.Managers
/// Used by Application.Services
/// </summary>
public interface IKeycloakManager
{
    /// <summary>
    /// Creates a new user in Keycloak
    /// Returns the userId if successful, null otherwise
    /// </summary>
    Task<string?> CreateUserAsync(User user);
    
    /// <summary>
    /// Authenticates a user with username and password using Keycloak's Direct Access Grant (Password Flow)
    /// Returns token response with access_token, token_type, expires_in, and optional refresh_token
    /// </summary>
    Task<(string AccessToken, string TokenType, int ExpiresIn, string? RefreshToken)?> AuthenticateUserAsync(string username, string password);
    
    /// <summary>
    /// Fetches user information from Keycloak's userinfo endpoint using the provided access token
    /// Returns strongly-typed Keycloak token claims from Keycloak
    /// </summary>
    Task<KeycloakTokenClaims?> GetUserInfoFromKeycloakAsync(string accessToken);
}
