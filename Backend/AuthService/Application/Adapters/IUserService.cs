using AuthService.API.DTOs.Requests;
using AuthService.API.DTOs.Responses;
using AuthService.Domain.Entities;

namespace AuthService.Application.Adapters;

/// <summary>
/// Application service interface for user management operations.
/// Handles user CRUD — has nothing to do with tokens or authentication.
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Registers a new user: validates uniqueness, hashes password via ICryptoManager, stores in DB.
    /// Assigns the default "User" role.
    /// </summary>
    Task<RegisterResponse> RegisterAsync(RegisterRequest request);

    /// <summary>
    /// Gets a user by their unique ID, including roles. Returns null if not found.
    /// </summary>
    Task<UserEntity?> GetByIdAsync(Guid userId);

    /// <summary>
    /// Gets a user by username (case-insensitive), including roles. Returns null if not found.
    /// </summary>
    Task<UserEntity?> GetByUsernameAsync(string username);

    /// <summary>
    /// Deactivates a user account (soft delete). Prevents future logins.
    /// </summary>
    Task<bool> DeactivateAsync(Guid userId);

    /// <summary>
    /// Gets the list of role names assigned to a user.
    /// </summary>
    List<string> GetUserRoles(UserEntity user);

    /// <summary>
    /// Builds a list of ClaimDtos from a user entity and their roles.
    /// Used by both AuthenticationService and TokenService to construct responses.
    /// </summary>
    List<ClaimDto> BuildClaimDtos(UserEntity user, List<string> roles);
}