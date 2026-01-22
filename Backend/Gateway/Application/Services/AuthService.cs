using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Gateway.API.DTOs.Requests;
using Gateway.API.DTOs.Responses;
using Gateway.Application.Adapters;
using Gateway.Domain.Models;
using Gateway.Infrastructure.Adapters;
using Microsoft.AspNetCore.Http;

namespace Gateway.Application.Services;

/// <summary>
/// Implementation of IAuthService
/// Contains business logic for authentication operations
/// Depends on Infrastructure adapters (IKeycloakManager)
/// </summary>
public class AuthService(
    IKeycloakManager keycloakManager,
    IHttpContextAccessor httpContextAccessor,
    ILogger<AuthService> logger) : IAuthService
{
    private readonly IKeycloakManager _keycloakManager = keycloakManager;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly ILogger<AuthService> _logger = logger;

    public async Task<UserInfoResponse> GetUserInfoAsync(string? authorizationHeader, bool fetchFromKeycloak = false)
    {
        var httpContext = _httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("HTTP context is not available");

        // Option 1: Fetch fresh data from Keycloak's userinfo endpoint
        if (fetchFromKeycloak && !string.IsNullOrEmpty(authorizationHeader) &&
            authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var token = authorizationHeader.Substring("Bearer ".Length).Trim();
            var keycloakClaims = await _keycloakManager.GetUserInfoFromKeycloakAsync(token).ConfigureAwait(false);

            if (keycloakClaims != null)
            {
                // Convert KeycloakTokenClaims to ClaimDto list for API response
                var claims = new List<ClaimDto>();

                // Add all non-null claims
                claims.Add(new ClaimDto { Type = "sub", Value = keycloakClaims.Sub });
                if (!string.IsNullOrEmpty(keycloakClaims.Name))
                    claims.Add(new ClaimDto { Type = "name", Value = keycloakClaims.Name });
                if (!string.IsNullOrEmpty(keycloakClaims.GivenName))
                    claims.Add(new ClaimDto { Type = "given_name", Value = keycloakClaims.GivenName });
                if (!string.IsNullOrEmpty(keycloakClaims.FamilyName))
                    claims.Add(new ClaimDto { Type = "family_name", Value = keycloakClaims.FamilyName });
                if (!string.IsNullOrEmpty(keycloakClaims.Email))
                    claims.Add(new ClaimDto { Type = "email", Value = keycloakClaims.Email });
                if (keycloakClaims.EmailVerified.HasValue)
                    claims.Add(new ClaimDto { Type = "email_verified", Value = keycloakClaims.EmailVerified.Value.ToString() });
                if (!string.IsNullOrEmpty(keycloakClaims.PreferredUsername))
                    claims.Add(new ClaimDto { Type = "preferred_username", Value = keycloakClaims.PreferredUsername });

                // Add roles from realm_access
                if (keycloakClaims.RealmAccess?.Roles != null)
                {
                    foreach (var role in keycloakClaims.RealmAccess.Roles)
                    {
                        claims.Add(new ClaimDto { Type = "role", Value = role });
                    }
                }

                // Add any custom claims
                if (keycloakClaims.CustomClaims != null)
                {
                    foreach (var customClaim in keycloakClaims.CustomClaims)
                    {
                        claims.Add(new ClaimDto { Type = customClaim.Key, Value = customClaim.Value?.ToString() ?? string.Empty });
                    }
                }

                _logger.LogInformation("Successfully fetched user info from Keycloak for user: {Sub}", keycloakClaims.Sub);

                return new UserInfoResponse
                {
                    IsAuthenticated = true,
                    Username = keycloakClaims.PreferredUsername ?? keycloakClaims.Sub,
                    AuthenticationType = "Bearer",
                    Claims = claims
                };
            }

            _logger.LogWarning("Failed to fetch user info from Keycloak, falling back to token claims");
        }

        // Option 2: Use claims from the token (already validated by middleware)
        // Authentication middleware already validated the token and set HttpContext.User
        var user = httpContext.User;
        var identity = user?.Identity;
        var tokenClaims = user?.Claims
            .Select(c => new ClaimDto { Type = c.Type, Value = c.Value })
            .ToList() ?? [];

        return new UserInfoResponse
        {
            IsAuthenticated = identity?.IsAuthenticated ?? false,
            Username = identity?.Name,
            AuthenticationType = identity?.AuthenticationType,
            Claims = tokenClaims
        };
    }

    public Task<LogoutResponse> LogoutAsync(string? authorizationHeader, bool isAuthenticated, string? username)
    {
        // Try JWT Bearer logout
        if (!string.IsNullOrEmpty(authorizationHeader) &&
            authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("JWT Bearer logout for user: {User}", username ?? "Unknown");

            return Task.FromResult(new LogoutResponse
            {
                Message = "Logged out successfully. Please discard your access token.",
                Username = username
            });
        }

        // Cookie/OIDC logout
        if (isAuthenticated)
        {
            _logger.LogInformation("Cookie/OIDC logout for user: {User}", username ?? "Unknown");
            return Task.FromResult(new LogoutResponse { Message = "Logged out successfully" });
        }

        _logger.LogWarning("Logout attempted but user not authenticated");
        return Task.FromResult(new LogoutResponse { Message = "Not authenticated" });
    }

    public async Task<RegisterResponse> RegisterUserAsync(RegisterRequest request)
    {
        _logger.LogInformation("Attempting to register user: {Username}", request.Username);

        try
        {
            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Password = request.Password
            };

            var userId = await _keycloakManager.CreateUserAsync(user).ConfigureAwait(false);

            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogError("User registration failed for: {Username}", request.Username);
                return new RegisterResponse
                {
                    Message = "Registration failed. Please try again or contact support.",
                    UserId = null
                };
            }

            _logger.LogInformation("User registered successfully: {Username} (ID: {UserId})",
                request.Username, userId);

            return new RegisterResponse
            {
                Message = "Account created successfully! You can now log in.",
                UserId = userId
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during user registration for: {Username}", request.Username);
            return new RegisterResponse
            {
                Message = ex.Message.Contains("exists")
                    ? "Username or email already exists. Please try a different one."
                    : "Registration failed. Please try again later.",
                UserId = null
            };
        }
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        _logger.LogInformation("Processing login request for user: {Username}", request.Username);

        try
        {
            // Step 1: Authenticate with Keycloak using password grant
            var tokenResult = await _keycloakManager.AuthenticateUserAsync(request.Username, request.Password).ConfigureAwait(false);

            if (tokenResult == null)
            {
                _logger.LogWarning("Authentication failed for user: {Username}", request.Username);
                return null;
            }

            var (accessToken, tokenType, expiresIn, refreshToken) = tokenResult.Value;

            // Step 2: Decode JWT and deserialize payload directly into KeycloakTokenClaims
            KeycloakTokenClaims? keycloakClaims = null;
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jsonToken = handler.ReadJwtToken(accessToken);

                // Extract payload JSON from JWT (payload is base64url encoded)
                var payloadBase64 = jsonToken.RawPayload;
                // Convert base64url to base64, then decode
                var base64 = payloadBase64.Replace('-', '+').Replace('_', '/');
                switch (base64.Length % 4)
                {
                    case 2: base64 += "=="; break;
                    case 3: base64 += "="; break;
                }
                var payloadBytes = Convert.FromBase64String(base64);
                var payloadJson = Encoding.UTF8.GetString(payloadBytes);

                // Deserialize directly into KeycloakTokenClaims - no manual mapping needed!
                keycloakClaims = JsonSerializer.Deserialize<KeycloakTokenClaims>(payloadJson);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token decode/deserialize failed for user: {Username}", request.Username);
                return null;
            }

            if (keycloakClaims == null || string.IsNullOrEmpty(keycloakClaims.Sub))
            {
                _logger.LogWarning("Failed to deserialize token claims or missing 'sub' for user: {Username}", request.Username);
                return null;
            }

            // Step 4: Build user info response from strongly-typed claims
            var claimDtos = new List<ClaimDto>
            {
                new ClaimDto { Type = "sub", Value = keycloakClaims.Sub }
            };
            if (!string.IsNullOrEmpty(keycloakClaims.Name))
                claimDtos.Add(new ClaimDto { Type = "name", Value = keycloakClaims.Name });
            if (!string.IsNullOrEmpty(keycloakClaims.GivenName))
                claimDtos.Add(new ClaimDto { Type = "given_name", Value = keycloakClaims.GivenName });
            if (!string.IsNullOrEmpty(keycloakClaims.FamilyName))
                claimDtos.Add(new ClaimDto { Type = "family_name", Value = keycloakClaims.FamilyName });
            if (!string.IsNullOrEmpty(keycloakClaims.Email))
                claimDtos.Add(new ClaimDto { Type = "email", Value = keycloakClaims.Email });
            if (!string.IsNullOrEmpty(keycloakClaims.PreferredUsername))
                claimDtos.Add(new ClaimDto { Type = "preferred_username", Value = keycloakClaims.PreferredUsername });

            // Add roles from realm_access
            if (keycloakClaims.RealmAccess?.Roles != null)
            {
                foreach (var role in keycloakClaims.RealmAccess.Roles)
                {
                    claimDtos.Add(new ClaimDto { Type = "role", Value = role });
                }
            }

            var userInfo = new UserInfoResponse
            {
                IsAuthenticated = true,
                Username = keycloakClaims.PreferredUsername ?? keycloakClaims.Sub,
                AuthenticationType = "Bearer",
                Claims = claimDtos
            };

            _logger.LogInformation("Login successful for user: {Username}", request.Username);

            return new LoginResponse
            {
                AccessToken = accessToken,
                TokenType = tokenType,
                ExpiresIn = expiresIn,
                RefreshToken = refreshToken,
                UserInfo = userInfo
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during login for user: {Username}", request.Username);
            return null;
        }
    }
}
