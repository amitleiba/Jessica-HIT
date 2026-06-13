using AuthService.API.DTOs.Requests;
using AuthService.Application.Adapters;
using AuthService.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.API.Controllers;

[ApiController]
[Route("api/Auth/[controller]")]
[Authorize(Roles = RoleType.Admin)]
public class UsersController(
    IUserService userService,
    ILogger<UsersController> logger) : ControllerBase
{
    private readonly IUserService _userService = userService;
    private readonly ILogger<UsersController> _logger = logger;

    /// <summary>
    /// Gets all users, including their roles.
    /// Accessible only to Admins.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllUsers()
    {
        _logger.LogInformation("Admin fetching all users list");

        var users = await _userService.GetAllUsersAsync().ConfigureAwait(false);

        var result = users.Select(u => new
        {
            u.Id,
            u.Username,
            u.Email,
            u.FirstName,
            u.LastName,
            u.IsActive,
            Roles = _userService.GetUserRoles(u)
        });

        return Ok(result);
    }

    /// <summary>
    /// Admin creates a new user and assigns a role.
    /// Accessible only to Admins.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        _logger.LogInformation("Admin creating user: {Username} with role: {Role}", request.Username, request.Role);

        // Validate that the role exists
        if (!RoleType.All.Contains(request.Role))
        {
            return BadRequest(new { message = $"Invalid role: {request.Role}. Allowed roles are: {string.Join(", ", RoleType.All)}" });
        }

        var result = await _userService.CreateUserWithRoleAsync(request).ConfigureAwait(false);

        if (result.UserId != null)
        {
            return Ok(result);
        }

        return BadRequest(result);
    }

    /// <summary>
    /// Admin deletes a user account.
    /// Accessible only to Admins.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        _logger.LogInformation("Admin deleting user ID: {UserId}", id);

        // Prevent admin from deleting themselves — fail closed if claim is missing/invalid
        var currentUserIdClaim = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);

        if (currentUserIdClaim == null || !Guid.TryParse(currentUserIdClaim.Value, out var currentUserId))
        {
            return Forbid();
        }

        if (currentUserId == id)
        {
            return BadRequest(new { message = "You cannot delete your own admin account." });
        }

        var deleted = await _userService.DeleteUserAsync(id).ConfigureAwait(false);

        if (deleted)
        {
            return Ok(new { message = "User deleted successfully" });
        }

        return NotFound(new { message = "User not found" });
    }

    /// <summary>
    /// Admin updates a user's role.
    /// Accessible only to Admins.
    /// </summary>
    [HttpPut("{id}/role")]
    public async Task<IActionResult> UpdateUserRole(Guid id, [FromBody] UpdateUserRoleRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        _logger.LogInformation("Admin changing role of user ID: {UserId} to {Role}", id, request.Role);

        // Validate that the role exists
        if (!RoleType.All.Contains(request.Role))
        {
            return BadRequest(new { message = $"Invalid role: {request.Role}. Allowed roles are: {string.Join(", ", RoleType.All)}" });
        }

        // Prevent admin from changing their own role — fail closed if claim is missing/invalid
        var currentUserIdClaim = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);

        if (currentUserIdClaim == null || !Guid.TryParse(currentUserIdClaim.Value, out var currentUserId))
        {
            return Forbid();
        }

        if (currentUserId == id)
        {
            return BadRequest(new { message = "You cannot change your own admin role." });
        }

        var updated = await _userService.UpdateUserRoleAsync(id, request.Role).ConfigureAwait(false);

        if (updated)
        {
            return Ok(new { message = "User role updated successfully" });
        }

        return NotFound(new { message = "User not found" });
    }
}
