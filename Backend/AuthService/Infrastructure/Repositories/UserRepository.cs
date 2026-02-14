using AuthService.Domain.Entities;
using AuthService.Domain.Enums;
using AuthService.Infrastructure.Adapters;
using AuthService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of IUserRepository.
/// All queries eagerly include UserRoles → Role for role-aware operations.
/// Uses EF.Functions.ILike() for case-insensitive PostgreSQL comparisons.
/// </summary>
public class UserRepository(
    AuthDbContext dbContext,
    ILogger<UserRepository> logger) : IUserRepository
{
    private readonly AuthDbContext _db = dbContext;
    private readonly ILogger<UserRepository> _logger = logger;

    public async Task<UserEntity?> GetByIdAsync(Guid id)
    {
        _logger.LogDebug("Fetching user by ID: {UserId}", id);

        return await _db.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == id)
            .ConfigureAwait(false);
    }

    public async Task<UserEntity?> GetByUsernameAsync(string username)
    {
        _logger.LogDebug("Fetching user by username: {Username}", username);

        return await _db.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => EF.Functions.ILike(u.Username, username))
            .ConfigureAwait(false);
    }

    public async Task<UserEntity?> GetByEmailAsync(string email)
    {
        _logger.LogDebug("Fetching user by email: {Email}", email);

        return await _db.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => EF.Functions.ILike(u.Email, email))
            .ConfigureAwait(false);
    }

    public async Task<bool> UsernameExistsAsync(string username)
    {
        return await _db.Users
            .AnyAsync(u => EF.Functions.ILike(u.Username, username))
            .ConfigureAwait(false);
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        return await _db.Users
            .AnyAsync(u => EF.Functions.ILike(u.Email, email))
            .ConfigureAwait(false);
    }

    public async Task<UserEntity> CreateAsync(UserEntity user)
    {
        _logger.LogInformation("Creating user: {Username}", user.Username);

        // Assign the default "User" role
        var defaultRole = await _db.Roles
            .FirstOrDefaultAsync(r => r.Name == RoleType.User)
            .ConfigureAwait(false);

        if (defaultRole != null)
        {
            user.UserRoles.Add(new UserRoleEntity
            {
                UserId = user.Id,
                RoleId = defaultRole.Id
            });
        }
        else
        {
            _logger.LogWarning("Default role '{RoleName}' not found — user created without roles", RoleType.User);
        }

        _db.Users.Add(user);
        await _db.SaveChangesAsync().ConfigureAwait(false);

        _logger.LogInformation("User created successfully: {Username} (ID: {UserId})", user.Username, user.Id);

        return user;
    }

    public async Task UpdateAsync(UserEntity user)
    {
        _logger.LogInformation("Updating user: {Username} (ID: {UserId})", user.Username, user.Id);

        user.UpdatedAt = DateTime.UtcNow;
        _db.Users.Update(user);
        await _db.SaveChangesAsync().ConfigureAwait(false);
    }
}