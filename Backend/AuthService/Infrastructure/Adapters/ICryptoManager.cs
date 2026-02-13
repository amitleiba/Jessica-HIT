namespace AuthService.Infrastructure.Adapters;

/// <summary>
/// Infrastructure interface for password cryptographic operations ONLY.
/// Swap implementations to change hashing algorithms without touching business logic.
///
/// Current:  BCryptCryptoManager (BCrypt adaptive hashing)
/// Future:   Argon2CryptoManager, PBKDF2CryptoManager, etc.
///
/// To swap algorithms: change ONE line in DependencyInjectionExtensions.cs
/// </summary>
public interface ICryptoManager
{
    /// <summary>
    /// Hashes a plaintext password using the configured algorithm.
    /// Returns the full hash string (includes salt + algorithm metadata).
    /// </summary>
    string HashPassword(string plainPassword);

    /// <summary>
    /// Verifies a plaintext password against a previously stored hash.
    /// Returns true if they match, false otherwise.
    /// </summary>
    bool VerifyPassword(string plainPassword, string storedHash);
}
