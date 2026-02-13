namespace AuthService.Domain.Entities;

/// <summary>
/// Database entity representing a refresh token.
/// The token itself is stored as a hash (via ICryptoManager) — never plaintext.
/// Supports rotation: when a token is used, it is revoked and a new one issued.
/// </summary>
public class RefreshTokenEntity
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    /// <summary>
    /// Hashed version of the refresh token (hashed via ICryptoManager.HashPassword).
    /// The raw token is only ever sent to the client, never stored.
    /// </summary>
    public required string TokenHash { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When non-null, this token has been revoked and cannot be used.
    /// </summary>
    public DateTime? RevokedAt { get; set; }

    public bool IsRevoked => RevokedAt.HasValue;

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

    /// <summary>
    /// A token is active only if it has not been revoked AND has not expired.
    /// </summary>
    public bool IsActive => !IsRevoked && !IsExpired;

    // ── Navigation Properties ──
    public UserEntity User { get; set; } = null!;
}

