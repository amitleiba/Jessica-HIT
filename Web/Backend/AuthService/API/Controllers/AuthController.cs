using AuthService.API.DTOs.Requests;
using AuthService.Application.Adapters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace AuthService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(
    IAuthenticationService authService,
    IUserService userService,
    ITokenService tokenService,
    ILogger<AuthController> logger) : ControllerBase
{
    private readonly IAuthenticationService _authService = authService;
    private readonly IUserService _userService = userService;
    private readonly ITokenService _tokenService = tokenService;
    private readonly ILogger<AuthController> _logger = logger;

    /// <summary>
    /// Authenticates user with username and password.
    /// Returns JWT access token + refresh token + user info.
    /// Delegates to: IAuthenticationService (auth orchestration)
    /// Frontend contract: POST /api/auth/login
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        _logger.LogInformation("Login request for username: {Username}", request.Username);

        var result = await _authService.LoginAsync(request).ConfigureAwait(false);

        if (result == null)
        {
            _logger.LogWarning("Login failed for username: {Username}", request.Username);
            return Unauthorized(new { message = "Invalid username or password" });
        }

        return Ok(result);
    }

    /// <summary>
    /// Registers a new user account.
    /// Delegates to: IUserService (user CRUD)
    /// Frontend contract: POST /api/auth/register
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        _logger.LogInformation("Registration request for username: {Username}", request.Username);

        var result = await _userService.RegisterAsync(request).ConfigureAwait(false);

        if (result.UserId != null)
        {
            return Ok(result);
        }

        return BadRequest(result);
    }

    /// <summary>
    /// Refreshes an expired access token using a valid refresh token.
    /// Uses IAuthenticationService to extract userId from expired token,
    /// then ITokenService to rotate the refresh token.
    /// Frontend contract: POST /api/auth/refresh
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        _logger.LogInformation("Token refresh request received");

        // Extract userId from the expired access token (IAuthenticationService validates signature, ignores expiry)
        var userId = _authService.ExtractUserIdFromExpiredToken(request.AccessToken);

        if (userId == null)
        {
            _logger.LogWarning("Token refresh failed — could not extract userId from expired access token");
            return Unauthorized(new { message = "Invalid access token" });
        }

        // Rotate refresh token (ITokenService handles hash verification + rotation)
        var result = await _tokenService.RefreshAsync(request.RefreshToken, userId.Value).ConfigureAwait(false);

        if (result == null)
        {
            _logger.LogWarning("Token refresh failed — refresh token invalid or expired");
            return Unauthorized(new { message = "Invalid or expired refresh token" });
        }

        return Ok(result);
    }

    /// <summary>
    /// Returns the current user's authentication info and claims.
    /// Requires a valid JWT access token in the Authorization header.
    /// Delegates to: IAuthenticationService (user info lookup)
    /// Frontend contract: GET /api/auth/user-info
    /// </summary>
    [HttpGet("user-info")]
    [Authorize]
    public async Task<IActionResult> GetUserInfo()
    {
        var userId = GetUserIdFromClaims();

        if (userId == null)
        {
            _logger.LogWarning("user-info request but no valid userId in claims");
            return Unauthorized(new { message = "Invalid token" });
        }

        var result = await _authService.GetUserInfoAsync(userId.Value).ConfigureAwait(false);
        return Ok(result);
    }

    /// <summary>
    /// Logs out the current user by revoking all their refresh tokens.
    /// Requires a valid JWT access token in the Authorization header.
    /// Delegates to: IAuthenticationService (logout + token revocation)
    /// Frontend contract: GET /api/auth/logout
    /// </summary>
    [HttpGet("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var userId = GetUserIdFromClaims();
        var username = User.FindFirst("preferred_username")?.Value;

        if (userId == null)
        {
            _logger.LogWarning("Logout request but no valid userId in claims");
            return Unauthorized(new { message = "Not authenticated" });
        }

        var result = await _authService.LogoutAsync(userId.Value, username).ConfigureAwait(false);
        return Ok(result);
    }

    /// <summary>
    /// Extracts roles from the current user's access token.
    /// Uses IAuthenticationService.ExtractRolesFromToken to parse claims.
    /// Frontend contract: GET /api/auth/roles
    /// </summary>
    [HttpGet("roles")]
    [Authorize]
    public IActionResult GetRoles()
    {
        var authHeader = Request.Headers.Authorization.ToString();

        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return Unauthorized(new { message = "Missing or invalid Authorization header" });
        }

        var token = authHeader["Bearer ".Length..].Trim();
        var roles = _authService.ExtractRolesFromToken(token);

        return Ok(new { Roles = roles });
    }

    // ── Private Helper ──

    /// <summary>
    /// Extracts the Guid userId from the JWT "sub" claim in the current HttpContext.
    /// </summary>
    private Guid? GetUserIdFromClaims()
    {
        var subClaim = User.FindFirst(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirst(ClaimTypes.NameIdentifier);

        if (subClaim != null && Guid.TryParse(subClaim.Value, out var userId))
        {
            return userId;
        }

        return null;
    }
}