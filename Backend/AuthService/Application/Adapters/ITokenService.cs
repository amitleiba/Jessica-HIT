using AuthService.API.DTOs.Responses;
using AuthService.Domain.Entities;

namespace AuthService.Application.Adapters;

/// <summary>
/// Application service interface for token operations.
/// Generates JWT access tokens and manages refresh token lifecycle.
/// Does NOT validate tokens or extract claims — that's IAuthenticationService.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Returns the configured access token expiry in seconds.
    /// </summary>
    int AccessTokenExpirySeconds { get; }

    /// <summary>
    /// Generates a signed JWT access token containing user claims and roles.
    /// </summary>
    string GenerateAccessToken(UserEntity user, IEnumerable<string> roles);

    /// <summary>
    /// Generates a new refresh token, hashes it via ICryptoManager, stores the hash in DB.
    /// Returns the raw (unhashed) token to send to the client.
    /// </summary>
    Task<string> GenerateRefreshTokenAsync(Guid userId);

    /// <summary>
    /// Validates an incoming refresh token for a specific user, rotates it (revoke old + issue new pair).
    /// The userId is extracted from the expired access token by IAuthenticationService.
    /// Returns a new LoginResponse with fresh tokens, or null if invalid/expired.
    /// </summary>
    Task<LoginResponse?> RefreshAsync(string rawRefreshToken, Guid userId);
}