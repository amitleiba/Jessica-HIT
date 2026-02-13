using AuthService.Domain.Entities;
using AuthService.Infrastructure.Adapters;
using AuthService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of IRefreshTokenRepository.
/// Handles refresh token CRUD and cleanup.
/// </summary>
public class RefreshTokenRepository(
    AuthDbContext dbContext,
    ILogger<RefreshTokenRepository> logger) : IRefreshTokenRepository
{
    private readonly AuthDbContext _db = dbContext;
    private readonly ILogger<RefreshTokenRepository> _logger = logger;

    public async Task CreateAsync(RefreshTokenEntity refreshToken)
    {
        _logger.LogDebug("Storing refresh token for user: {UserId}", refreshToken.UserId);

        _db.RefreshTokens.Add(refreshToken);
        await _db.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task<List<RefreshTokenEntity>> GetActiveTokensByUserIdAsync(Guid userId)
    {
        _logger.LogDebug("Fetching active refresh tokens for user: {UserId}", userId);

        return await _db.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null && rt.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(rt => rt.CreatedAt)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    public async Task RevokeAsync(RefreshTokenEntity token)
    {
        _logger.LogDebug("Revoking refresh token: {TokenId}", token.Id);

        token.RevokedAt = DateTime.UtcNow;
        _db.RefreshTokens.Update(token);
        await _db.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task RevokeAllForUserAsync(Guid userId)
    {
        _logger.LogInformation("Revoking ALL active refresh tokens for user: {UserId}", userId);

        var activeTokens = await _db.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null)
            .ToListAsync()
            .ConfigureAwait(false);

        foreach (var token in activeTokens)
        {
            token.RevokedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync().ConfigureAwait(false);

        _logger.LogInformation("Revoked {Count} refresh tokens for user: {UserId}", activeTokens.Count, userId);
    }

    public async Task CleanupExpiredTokensAsync(DateTime cutoffDate)
    {
        _logger.LogInformation("Cleaning up expired/revoked refresh tokens older than: {Cutoff}", cutoffDate);

        var staleTokens = await _db.RefreshTokens
            .Where(rt => (rt.RevokedAt != null || rt.ExpiresAt < DateTime.UtcNow) && rt.CreatedAt < cutoffDate)
            .ToListAsync()
            .ConfigureAwait(false);

        _db.RefreshTokens.RemoveRange(staleTokens);
        await _db.SaveChangesAsync().ConfigureAwait(false);

        _logger.LogInformation("Cleaned up {Count} stale refresh tokens", staleTokens.Count);
    }
}

