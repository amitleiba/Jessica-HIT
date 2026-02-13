using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AuthService.API.DTOs.Responses;
using AuthService.Application.Adapters;
using AuthService.Domain.Configuration;
using AuthService.Domain.Entities;
using AuthService.Infrastructure.Adapters;
using Microsoft.IdentityModel.Tokens;

namespace AuthService.Application.Services;

/// <summary>
/// Implementation of ITokenService.
/// Handles JWT creation (HMAC-SHA256) and refresh token lifecycle with rotation.
/// Uses ICryptoManager for password-level hashing of stored refresh tokens.
/// Generates its own cryptographically secure random tokens via CSPRNG.
/// Does NOT validate tokens or extract claims — that's IAuthenticationService.
/// </summary>
public class TokenService(
    JwtConfig jwtConfig,
    ICryptoManager cryptoManager,
    IRefreshTokenRepository refreshTokenRepository,
    IUserService userService,
    ILogger<TokenService> logger) : ITokenService
{
    private readonly JwtConfig _jwtConfig = jwtConfig;
    private readonly ICryptoManager _cryptoManager = cryptoManager;
    private readonly IRefreshTokenRepository _refreshTokenRepo = refreshTokenRepository;
    private readonly IUserService _userService = userService;
    private readonly ILogger<TokenService> _logger = logger;

    public int AccessTokenExpirySeconds => _jwtConfig.AccessTokenExpiryMinutes * 60;

    // ============================================
    // Access Token Generation (JWT, HMAC-SHA256)
    // ============================================

    public string GenerateAccessToken(UserEntity user, IEnumerable<string> roles)
    {
        _logger.LogInformation("Generating access token for user: {Username}", user.Username);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("preferred_username", user.Username),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.GivenName, user.FirstName),
            new(JwtRegisteredClaimNames.FamilyName, user.LastName),
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim("role", role));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtConfig.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtConfig.Issuer,
            audience: _jwtConfig.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtConfig.AccessTokenExpiryMinutes),
            signingCredentials: credentials
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        _logger.LogDebug("Access token generated. Expires in {Minutes} minutes", _jwtConfig.AccessTokenExpiryMinutes);

        return tokenString;
    }

    // ============================================
    // Refresh Token Generation + Storage
    // ============================================

    public async Task<string> GenerateRefreshTokenAsync(Guid userId)
    {
        _logger.LogDebug("Generating refresh token for user: {UserId}", userId);

        // Generate cryptographically secure random token (CSPRNG)
        var rawToken = GenerateSecureToken();

        // Hash the token before storing — never store raw refresh tokens in DB
        var tokenHash = _cryptoManager.HashPassword(rawToken);

        var refreshTokenEntity = new RefreshTokenEntity
        {
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtConfig.RefreshTokenExpiryDays),
            CreatedAt = DateTime.UtcNow
        };

        await _refreshTokenRepo.CreateAsync(refreshTokenEntity).ConfigureAwait(false);
        _logger.LogDebug("Refresh token stored (hashed). Expires in {Days} days", _jwtConfig.RefreshTokenExpiryDays);

        // Return the RAW token to the client — they'll send it back on refresh
        return rawToken;
    }

    // ============================================
    // Refresh Token Rotation
    // ============================================

    public async Task<LoginResponse?> RefreshAsync(string rawRefreshToken, Guid userId)
    {
        _logger.LogInformation("Processing refresh token for user: {UserId}", userId);

        // Step 1: Get this user's active refresh tokens
        var activeTokens = await _refreshTokenRepo
            .GetActiveTokensByUserIdAsync(userId)
            .ConfigureAwait(false);

        if (activeTokens.Count == 0)
        {
            _logger.LogWarning("No active refresh tokens found for user: {UserId}", userId);
            return null;
        }

        // Step 2: Find the matching token by verifying the hash
        RefreshTokenEntity? matchedToken = null;
        foreach (var token in activeTokens)
        {
            if (_cryptoManager.VerifyPassword(rawRefreshToken, token.TokenHash))
            {
                matchedToken = token;
                break;
            }
        }

        if (matchedToken == null)
        {
            _logger.LogWarning("Refresh token hash mismatch for user: {UserId} — possible token reuse attack", userId);
            return null;
        }

        // Step 3: Revoke the used token (rotation: one-time use)
        await _refreshTokenRepo.RevokeAsync(matchedToken).ConfigureAwait(false);
        _logger.LogInformation("Old refresh token revoked for user: {UserId}", userId);

        // Step 4: Load the user with roles (via UserService)
        var user = await _userService.GetByIdAsync(userId).ConfigureAwait(false);
        if (user == null || !user.IsActive)
        {
            _logger.LogWarning("User not found or inactive during refresh: {UserId}", userId);
            return null;
        }

        // Step 5: Generate new token pair
        var roles = _userService.GetUserRoles(user);
        var newAccessToken = GenerateAccessToken(user, roles);
        var newRefreshToken = await GenerateRefreshTokenAsync(user.Id).ConfigureAwait(false);

        var claims = _userService.BuildClaimDtos(user, roles);

        _logger.LogInformation("Token refresh successful for user: {Username}", user.Username);

        return new LoginResponse
        {
            AccessToken = newAccessToken,
            TokenType = "Bearer",
            ExpiresIn = AccessTokenExpirySeconds,
            RefreshToken = newRefreshToken,
            UserInfo = new UserInfoResponse
            {
                IsAuthenticated = true,
                Username = user.Username,
                AuthenticationType = "Bearer",
                Claims = claims
            }
        };
    }

    // ============================================
    // Secure Random Token Generation (CSPRNG)
    // ============================================

    /// <summary>
    /// Generates a cryptographically secure random token string (Base64).
    /// Used for refresh tokens — the raw value is sent to the client,
    /// while a BCrypt hash is stored in the database.
    /// </summary>
    private static string GenerateSecureToken(int byteLength = 64)
    {
        var randomBytes = new byte[byteLength];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }
}
