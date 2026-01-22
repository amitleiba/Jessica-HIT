using Gateway.API.DTOs.Requests;
using Gateway.API.DTOs.Responses;

namespace Gateway.Application.Adapters;

/// <summary>
/// Application service interface for authentication operations
/// Used by API Controllers
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Gets the current user's authentication information
    /// </summary>
    /// <param name="authorizationHeader">The Authorization header value (Bearer token)</param>
    /// <param name="fetchFromKeycloak">If true, fetches fresh user data from Keycloak's userinfo endpoint. If false, uses claims from the validated token.</param>
    Task<UserInfoResponse> GetUserInfoAsync(string? authorizationHeader, bool fetchFromKeycloak = false);
    
    /// <summary>
    /// Logs out the current user
    /// </summary>
    Task<LogoutResponse> LogoutAsync(string? authorizationHeader, bool isAuthenticated, string? username);
    
    /// <summary>
    /// Registers a new user in Keycloak
    /// </summary>
    Task<RegisterResponse> RegisterUserAsync(RegisterRequest request);
    
    /// <summary>
    /// Authenticates a user with username and password
    /// Returns access token and user information
    /// </summary>
    Task<LoginResponse?> LoginAsync(LoginRequest request);
}
