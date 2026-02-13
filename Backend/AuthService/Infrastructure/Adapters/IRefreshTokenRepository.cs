using AuthService.Domain.Entities;

namespace AuthService.Infrastructure.Adapters;

/// <summary>
/// Repository interface for RefreshToken database operations.
/// Implemented by Infrastructure.Repositories.RefreshTokenRepository.
/// Used by Application.Services.TokenService.
/// </summary>
public interface IRefreshTokenRepository
{
    /// <summary>
    /// Stores a new refresh token entity in the database.
    /// </summary>
    Task CreateAsync(RefreshTokenEntity refreshToken);

    /// <summary>
    /// Gets all active (non-revoked, non-expired) refresh tokens for a user.
    /// </summary>
    Task<List<RefreshTokenEntity>> GetActiveTokensByUserIdAsync(Guid userId);

    /// <summary>
    /// Revokes a specific refresh token by setting RevokedAt to UTC now.
    /// </summary>
    Task RevokeAsync(RefreshTokenEntity token);

    /// <summary>
    /// Revokes ALL active refresh tokens for a user (used on logout/password change).
    /// </summary>
    Task RevokeAllForUserAsync(Guid userId);

    /// <summary>
    /// Removes expired and revoked tokens older than the specified cutoff (housekeeping).
    /// </summary>
    Task CleanupExpiredTokensAsync(DateTime cutoffDate);
}

