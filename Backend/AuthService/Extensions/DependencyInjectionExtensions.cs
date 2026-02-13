using AuthService.Application.Adapters;
using AuthService.Application.Services;
using AuthService.Infrastructure.Adapters;
using AuthService.Infrastructure.Managers;
using AuthService.Infrastructure.Repositories;

namespace AuthService.Extensions;

/// <summary>
/// Extension methods for registering services with dependency injection.
/// Following Clean Architecture: Application → Infrastructure → Domain.
///
/// Service separation:
///   IUserService           → User CRUD (register, get, deactivate)
///   ITokenService          → Token generation + refresh rotation
///   IAuthenticationService → Login/logout, token validation, role extraction
///
/// To swap cryptography algorithm: change ONE line below (BCryptCryptoManager → YourNewManager).
/// </summary>
public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();

        // ── Configuration instances (for direct constructor injection) ──
        services.AddSingleton(sp => configuration.GetJwtConfig());
        services.AddSingleton(sp => configuration.GetCryptoConfig());

        // ── Infrastructure: Cryptography ──
        // *** SWAP ALGORITHM HERE: BCryptCryptoManager → Argon2CryptoManager, etc. ***
        services.AddSingleton<ICryptoManager, BCryptCryptoManager>();

        // ── Infrastructure: Repositories ──
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

        // ── Application: Services ──
        services.AddScoped<IUserService, UserService>();               // User CRUD
        services.AddScoped<ITokenService, TokenService>();             // Token generation + refresh
        services.AddScoped<IAuthenticationService, AuthenticationService>(); // Auth orchestration

        return services;
    }
}
