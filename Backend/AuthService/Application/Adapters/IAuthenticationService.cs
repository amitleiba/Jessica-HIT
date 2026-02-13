using System.Security.Claims;
using AuthService.API.DTOs.Requests;
using AuthService.API.DTOs.Responses;

namespace AuthService.Application.Adapters;

/// <summary>
/// Application service interface for authentication operations ONLY.
/// Orchestrates login/logout flows, validates tokens, and extracts roles.
/// Does NOT manage user data (that's IUserService) or generate tokens (that's ITokenService).
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Authenticates a user with username + password.
    /// Verifies credentials via ICryptoManager, generates tokens via ITokenService.
    /// Returns full login response or null if credentials are invalid.
    /// </summary>
    Task<LoginResponse?> LoginAsync(LoginRequest request);

    /// <summary>
    /// Logs out a user by revoking all their active refresh tokens.
    /// </summary>
    Task<LogoutResponse> LogoutAsync(Guid userId, string? username);

    /// <summary>
    /// Returns the current user's info by looking them up in the DB via their userId.
    /// </summary>
    Task<UserInfoResponse> GetUserInfoAsync(Guid userId);

    /// <summary>
    /// Validates a JWT access token using Microsoft's JwtSecurityTokenHandler.
    /// Returns the ClaimsPrincipal if valid, null if invalid/expired.
    /// </summary>
    ClaimsPrincipal? ValidateToken(string accessToken);

    /// <summary>
    /// Extracts the userId (sub claim) from an expired JWT WITHOUT validating expiry.
    /// Signature IS still validated. Used for the refresh token flow.
    /// </summary>
    Guid? ExtractUserIdFromExpiredToken(string expiredAccessToken);

    /// <summary>
    /// Extracts role claims from a valid JWT access token.
    /// Returns a list of role names, or empty list if token is invalid.
    /// </summary>
    List<string> ExtractRolesFromToken(string accessToken);
}
