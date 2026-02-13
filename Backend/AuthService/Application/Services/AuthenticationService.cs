using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AuthService.API.DTOs.Requests;
using AuthService.API.DTOs.Responses;
using AuthService.Application.Adapters;
using AuthService.Domain.Configuration;
using AuthService.Infrastructure.Adapters;
using Microsoft.IdentityModel.Tokens;

namespace AuthService.Application.Services;

/// <summary>
/// Implementation of IAuthenticationService.
/// Orchestrates authentication flows — login, logout, token validation, role extraction.
/// Delegates to: IUserService (user lookup), ICryptoManager (password verify), ITokenService (token generation).
/// Uses Microsoft's JwtSecurityTokenHandler for token validation and claim extraction.
/// </summary>
public class AuthenticationService(
    IUserService userService,
    ICryptoManager cryptoManager,
    ITokenService tokenService,
    IRefreshTokenRepository refreshTokenRepository,
    JwtConfig jwtConfig,
    ILogger<AuthenticationService> logger) : IAuthenticationService
{
    private readonly IUserService _userService = userService;
    private readonly ICryptoManager _cryptoManager = cryptoManager;
    private readonly ITokenService _tokenService = tokenService;
    private readonly IRefreshTokenRepository _refreshTokenRepo = refreshTokenRepository;
    private readonly JwtConfig _jwtConfig = jwtConfig;
    private readonly ILogger<AuthenticationService> _logger = logger;

    // ============================================
    // Login — Authenticate user + generate tokens
    // ============================================

    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        _logger.LogInformation("Processing login for user: {Username}", request.Username);

        try
        {
            // Step 1: Find user by username (via UserService)
            var user = await _userService.GetByUsernameAsync(request.Username).ConfigureAwait(false);

            if (user == null)
            {
                _logger.LogWarning("Login failed — user not found: {Username}", request.Username);
                return null;
            }

            if (!user.IsActive)
            {
                _logger.LogWarning("Login failed — user account is inactive: {Username}", request.Username);
                return null;
            }

            // Step 2: Verify password (via ICryptoManager)
            if (!_cryptoManager.VerifyPassword(request.Password, user.PasswordHash))
            {
                _logger.LogWarning("Login failed — invalid password for user: {Username}", request.Username);
                return null;
            }

            // Step 3: Generate tokens (via ITokenService)
            var roles = _userService.GetUserRoles(user);
            var accessToken = _tokenService.GenerateAccessToken(user, roles);
            var refreshToken = await _tokenService.GenerateRefreshTokenAsync(user.Id).ConfigureAwait(false);

            // Step 4: Build response (matches frontend contract)
            var claims = _userService.BuildClaimDtos(user, roles);

            _logger.LogInformation("Login successful for user: {Username}", request.Username);

            return new LoginResponse
            {
                AccessToken = accessToken,
                TokenType = "Bearer",
                ExpiresIn = _tokenService.AccessTokenExpirySeconds,
                RefreshToken = refreshToken,
                UserInfo = new UserInfoResponse
                {
                    IsAuthenticated = true,
                    Username = user.Username,
                    AuthenticationType = "Bearer",
                    Claims = claims
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during login for user: {Username}", request.Username);
            return null;
        }
    }

    // ============================================
    // Logout — Revoke all refresh tokens
    // ============================================

    public async Task<LogoutResponse> LogoutAsync(Guid userId, string? username)
    {
        _logger.LogInformation("Processing logout for user: {Username} ({UserId})", username ?? "Unknown", userId);

        try
        {
            await _refreshTokenRepo.RevokeAllForUserAsync(userId).ConfigureAwait(false);

            _logger.LogInformation("Logout successful — all refresh tokens revoked for user: {Username}", username ?? "Unknown");

            return new LogoutResponse
            {
                Message = "Logged out successfully. Please discard your access token.",
                Username = username
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during logout for user: {UserId}", userId);
            return new LogoutResponse
            {
                Message = "Logout completed with warnings.",
                Username = username
            };
        }
    }

    // ============================================
    // Get User Info — from DB by userId
    // ============================================

    public async Task<UserInfoResponse> GetUserInfoAsync(Guid userId)
    {
        _logger.LogDebug("Fetching user info for: {UserId}", userId);

        var user = await _userService.GetByIdAsync(userId).ConfigureAwait(false);

        if (user == null || !user.IsActive)
        {
            _logger.LogWarning("User not found or inactive: {UserId}", userId);
            return new UserInfoResponse
            {
                IsAuthenticated = false,
                Username = null,
                AuthenticationType = null,
                Claims = []
            };
        }

        var roles = _userService.GetUserRoles(user);
        var claims = _userService.BuildClaimDtos(user, roles);

        return new UserInfoResponse
        {
            IsAuthenticated = true,
            Username = user.Username,
            AuthenticationType = "Bearer",
            Claims = claims
        };
    }

    // ============================================
    // Token Validation — Full validation (incl. expiry)
    // ============================================

    public ClaimsPrincipal? ValidateToken(string accessToken)
    {
        _logger.LogDebug("Validating access token");

        try
        {
            var principal = ValidateJwt(accessToken, validateLifetime: true);

            if (principal != null)
            {
                var username = principal.FindFirst("preferred_username")?.Value ?? "Unknown";
                _logger.LogInformation("Token validated successfully for user: {Username}", username);
            }

            return principal;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Token validation failed");
            return null;
        }
    }

    // ============================================
    // Extract UserId from Expired Token (for refresh)
    // ============================================

    public Guid? ExtractUserIdFromExpiredToken(string expiredAccessToken)
    {
        _logger.LogDebug("Extracting userId from expired token");

        try
        {
            var principal = ValidateJwt(expiredAccessToken, validateLifetime: false);

            if (principal == null)
            {
                _logger.LogWarning("Could not validate expired token signature");
                return null;
            }

            var subClaim = principal.FindFirst(JwtRegisteredClaimNames.Sub)
                ?? principal.FindFirst(ClaimTypes.NameIdentifier);

            if (subClaim != null && Guid.TryParse(subClaim.Value, out var userId))
            {
                _logger.LogDebug("Extracted userId from expired token: {UserId}", userId);
                return userId;
            }

            _logger.LogWarning("Could not extract userId — 'sub' claim missing or invalid");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract userId from expired token");
            return null;
        }
    }

    // ============================================
    // Extract Roles from Token
    // ============================================

    public List<string> ExtractRolesFromToken(string accessToken)
    {
        _logger.LogDebug("Extracting roles from token");

        try
        {
            var principal = ValidateJwt(accessToken, validateLifetime: true);

            if (principal == null)
            {
                _logger.LogWarning("Cannot extract roles — token is invalid");
                return [];
            }

            var roles = principal.FindAll("role")
                .Select(c => c.Value)
                .ToList();

            _logger.LogDebug("Extracted {Count} roles from token: [{Roles}]",
                roles.Count, string.Join(", ", roles));

            return roles;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract roles from token");
            return [];
        }
    }

    // ============================================
    // PRIVATE: Shared JWT Validation Logic
    // ============================================

    /// <summary>
    /// Core JWT validation using Microsoft's JwtSecurityTokenHandler.
    /// Validates issuer, audience, signing key. Lifetime validation is configurable.
    /// </summary>
    private ClaimsPrincipal? ValidateJwt(string token, bool validateLifetime)
    {
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _jwtConfig.Issuer,
            ValidateAudience = true,
            ValidAudience = _jwtConfig.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_jwtConfig.SecretKey)),
            ValidateLifetime = validateLifetime,
            NameClaimType = "preferred_username",
            RoleClaimType = "role",
            ClockSkew = TimeSpan.FromMinutes(2)
        };

        var handler = new JwtSecurityTokenHandler();
        var principal = handler.ValidateToken(token, validationParameters, out _);
        return principal;
    }
}
