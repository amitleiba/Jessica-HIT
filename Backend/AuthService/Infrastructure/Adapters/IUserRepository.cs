using AuthService.Domain.Entities;

namespace AuthService.Infrastructure.Adapters;

/// <summary>
/// Repository interface for User database operations.
/// Implemented by Infrastructure.Repositories.UserRepository.
/// Used by Application.Services.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Gets a user by their unique ID, including roles.
    /// </summary>
    Task<UserEntity?> GetByIdAsync(Guid id);

    /// <summary>
    /// Gets a user by username (case-insensitive), including roles.
    /// </summary>
    Task<UserEntity?> GetByUsernameAsync(string username);

    /// <summary>
    /// Gets a user by email (case-insensitive), including roles.
    /// </summary>
    Task<UserEntity?> GetByEmailAsync(string email);

    /// <summary>
    /// Checks if a username is already taken.
    /// </summary>
    Task<bool> UsernameExistsAsync(string username);

    /// <summary>
    /// Checks if an email is already registered.
    /// </summary>
    Task<bool> EmailExistsAsync(string email);

    /// <summary>
    /// Creates a new user and assigns the default "User" role.
    /// Returns the created user entity with populated ID.
    /// </summary>
    Task<UserEntity> CreateAsync(UserEntity user);

    /// <summary>
    /// Updates an existing user entity.
    /// </summary>
    Task UpdateAsync(UserEntity user);

    /// <summary>
    /// Gets all users including roles.
    /// </summary>
    Task<List<UserEntity>> GetAllAsync();

    /// <summary>
    /// Creates a new user with the specified role.
    /// </summary>
    Task<UserEntity> CreateWithRoleAsync(UserEntity user, string roleName);

    /// <summary>
    /// Deletes a user by ID. Returns true if deleted, false if not found.
    /// </summary>
    Task<bool> DeleteAsync(Guid id);

    /// <summary>
    /// Updates a user's role. Returns true if successful.
    /// </summary>
    Task<bool> UpdateUserRoleAsync(Guid userId, string roleName);
}