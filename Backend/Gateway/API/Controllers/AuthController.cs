using Gateway.API.DTOs.Requests;
using Gateway.API.DTOs.Responses;
using Gateway.Application.Adapters;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(
    IAuthService authService,
    ILogger<AuthController> logger) : ControllerBase
{
    private readonly IAuthService _authService = authService;
    private readonly ILogger<AuthController> _logger = logger;


    /// <summary>
    /// Authenticates user with username and password (API-based flow)
    /// This is the secure backend proxy endpoint for SPAs
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> LoginWithCredentials([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        _logger.LogInformation("Password-based login request for username: {Username}", request.Username);

        var result = await _authService.LoginAsync(request).ConfigureAwait(false);

        if (result == null)
        {
            _logger.LogWarning("Login failed for username: {Username}", request.Username);
            return Unauthorized(new { message = "Invalid username or password" });
        }

        return Ok(result);
    }

    /// <summary>
    /// Logs out the current user from both the application and Keycloak
    /// </summary>
    [HttpGet("logout")]
    public async Task<IActionResult> Logout()
    {
        var authHeader = Request.Headers["Authorization"].ToString();
        var isAuthenticated = User?.Identity?.IsAuthenticated ?? false;
        var username = User?.Identity?.Name;

        var result = await _authService.LogoutAsync(authHeader, isAuthenticated, username).ConfigureAwait(false);

        // Perform actual sign out if using Cookie/OIDC
        if (!string.IsNullOrEmpty(authHeader) &&
            authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            // JWT Bearer - no server-side logout needed
            return Ok(result);
        }

        if (isAuthenticated)
        {
            // Cookie/OIDC - perform sign out
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme).ConfigureAwait(false);
            await HttpContext.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme).ConfigureAwait(false);
            return Ok(result);
        }

        return Unauthorized(result);
    }

    /// <summary>
    /// Returns the current user's authentication information and claims
    /// </summary>
    /// <param name="fromKeycloak">If true, fetches fresh user data from Keycloak's userinfo endpoint. If false (default), uses claims from the validated token.</param>
    [HttpGet("user-info")]
    public async Task<IActionResult> GetUserInfo([FromQuery] bool fromKeycloak = false)
    {
        var authHeader = Request.Headers["Authorization"].ToString();
        var result = await _authService.GetUserInfoAsync(authHeader, fromKeycloak).ConfigureAwait(false);
        return Ok(result);
    }

    /// <summary>
    /// Registers a new user in Keycloak
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

        var result = await _authService.RegisterUserAsync(request).ConfigureAwait(false);

        if (result.UserId != null)
        {
            return Ok(result);
        }

        return BadRequest(result);
    }
}
