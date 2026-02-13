using AuthService.API.DTOs.Requests;
using AuthService.API.DTOs.Responses;
using AuthService.Application.Adapters;
using AuthService.Domain.Entities;
using AuthService.Infrastructure.Adapters;

namespace AuthService.Application.Services;

/// <summary>
/// Implementation of IUserService.
/// Pure user data management — register, get, update, deactivate.
/// Delegates password hashing to ICryptoManager, never touches tokens.
/// </summary>
public class UserService(
    ICryptoManager cryptoManager,
    IUserRepository userRepository,
    ILogger<UserService> logger) : IUserService
{
    private readonly ICryptoManager _cryptoManager = cryptoManager;
    private readonly IUserRepository _userRepo = userRepository;
    private readonly ILogger<UserService> _logger = logger;

    // ============================================
    // Register (Create User)
    // ============================================

    public async Task<RegisterResponse> RegisterAsync(RegisterRequest request)
    {
        _logger.LogInformation("Attempting to register user: {Username}", request.Username);

        try
        {
            // Check if username already exists
            if (await _userRepo.UsernameExistsAsync(request.Username).ConfigureAwait(false))
            {
                _logger.LogWarning("Registration failed — username already exists: {Username}", request.Username);
                return new RegisterResponse
                {
                    Message = "Username already exists. Please try a different one.",
                    UserId = null
                };
            }

            // Check if email already exists
            if (await _userRepo.EmailExistsAsync(request.Email).ConfigureAwait(false))
            {
                _logger.LogWarning("Registration failed — email already exists: {Email}", request.Email);
                return new RegisterResponse
                {
                    Message = "Email already registered. Please use a different email.",
                    UserId = null
                };
            }

            // Hash password via ICryptoManager (BCrypt by default — swappable)
            var passwordHash = _cryptoManager.HashPassword(request.Password);

            var user = new UserEntity
            {
                Id = Guid.NewGuid(),
                Username = request.Username,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                PasswordHash = passwordHash,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var createdUser = await _userRepo.CreateAsync(user).ConfigureAwait(false);

            _logger.LogInformation("User registered successfully: {Username} (ID: {UserId})", request.Username, createdUser.Id);

            return new RegisterResponse
            {
                Message = "Account created successfully! You can now log in.",
                UserId = createdUser.Id.ToString()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during user registration for: {Username}", request.Username);
            return new RegisterResponse
            {
                Message = "Registration failed. Please try again later.",
                UserId = null
            };
        }
    }

    // ============================================
    // Read
    // ============================================

    public async Task<UserEntity?> GetByIdAsync(Guid userId)
    {
        _logger.LogDebug("Fetching user by ID: {UserId}", userId);
        return await _userRepo.GetByIdAsync(userId).ConfigureAwait(false);
    }

    public async Task<UserEntity?> GetByUsernameAsync(string username)
    {
        _logger.LogDebug("Fetching user by username: {Username}", username);
        return await _userRepo.GetByUsernameAsync(username).ConfigureAwait(false);
    }

    // ============================================
    // Deactivate (Soft Delete)
    // ============================================

    public async Task<bool> DeactivateAsync(Guid userId)
    {
        _logger.LogInformation("Deactivating user: {UserId}", userId);

        var user = await _userRepo.GetByIdAsync(userId).ConfigureAwait(false);

        if (user == null)
        {
            _logger.LogWarning("Deactivation failed — user not found: {UserId}", userId);
            return false;
        }

        user.IsActive = false;
        await _userRepo.UpdateAsync(user).ConfigureAwait(false);

        _logger.LogInformation("User deactivated: {Username} (ID: {UserId})", user.Username, userId);
        return true;
    }

    // ============================================
    // Helpers
    // ============================================

    public List<string> GetUserRoles(UserEntity user)
    {
        return user.UserRoles.Select(ur => ur.Role.Name).ToList();
    }

    public List<ClaimDto> BuildClaimDtos(UserEntity user, List<string> roles)
    {
        var claims = new List<ClaimDto>
        {
            new() { Type = "sub", Value = user.Id.ToString() },
            new() { Type = "preferred_username", Value = user.Username },
            new() { Type = "email", Value = user.Email },
            new() { Type = "given_name", Value = user.FirstName },
            new() { Type = "family_name", Value = user.LastName },
        };

        foreach (var role in roles)
        {
            claims.Add(new ClaimDto { Type = "role", Value = role });
        }

        return claims;
    }
}

