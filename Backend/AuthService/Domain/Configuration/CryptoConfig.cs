namespace AuthService.Domain.Configuration;

/// <summary>
/// Cryptography configuration for ICryptoManager (password hashing only).
/// Bound from appsettings.json "Crypto" section.
/// </summary>
public class CryptoConfig
{
    public const string SectionName = "Crypto";

    /// <summary>
    /// BCrypt work factor (cost parameter).
    /// Higher = slower + more secure. Recommended: 12 for production, 4 for tests.
    /// </summary>
    public int WorkFactor { get; set; } = 12;
}