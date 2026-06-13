using AuthService.Infrastructure.Adapters;
using CryptoConfiguration = AuthService.Domain.Configuration.CryptoConfig;

namespace AuthService.Infrastructure.Managers;

/// <summary>
/// Default ICryptoManager implementation — password hashing only.
/// 
/// Uses BCrypt adaptive hashing (includes salt, configurable work factor).
///
/// To swap algorithms: create a new implementation (e.g. Argon2CryptoManager)
/// and change the DI registration in DependencyInjectionExtensions.cs.
/// </summary>
public class BCryptCryptoManager(
    CryptoConfiguration cryptoConfig,
    ILogger<BCryptCryptoManager> logger) : ICryptoManager
{
    private readonly CryptoConfiguration _config = cryptoConfig;
    private readonly ILogger<BCryptCryptoManager> _logger = logger;

    public string HashPassword(string plainPassword)
    {
        _logger.LogDebug("Hashing password with BCrypt (WorkFactor: {WorkFactor})", _config.WorkFactor);
        return BCrypt.Net.BCrypt.HashPassword(plainPassword, workFactor: _config.WorkFactor);
    }

    public bool VerifyPassword(string plainPassword, string storedHash)
    {
        try
        {
            var isValid = BCrypt.Net.BCrypt.Verify(plainPassword, storedHash);
            _logger.LogDebug("Password verification result: {IsValid}", isValid);
            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Password verification threw an exception — hash may be corrupt");
            return false;
        }
    }
}