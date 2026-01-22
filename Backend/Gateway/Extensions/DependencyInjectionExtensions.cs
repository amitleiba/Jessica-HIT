using Gateway.Application.Adapters;
using Gateway.Application.Services;
using Gateway.Domain.Configuration;
using Gateway.Infrastructure.Adapters;
using Gateway.Infrastructure.Managers;

namespace Gateway.Extensions;

/// <summary>
/// Extension methods for registering services with dependency injection
/// Following Clean Architecture principles
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Registers all application and infrastructure services
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Register HttpContextAccessor (required by managers)
        services.AddHttpContextAccessor();

        // Register HttpClient (required by KeycloakManager for Admin API calls)
        services.AddHttpClient();

        // Register configuration instances (for direct injection)
        services.AddSingleton(sp => configuration.GetKeycloakConfig());

        // Register Application Services (implements Application.Adapters)
        services.AddScoped<IAuthService, AuthService>();

        // Register Infrastructure Managers (implements Infrastructure.Adapters)
        services.AddScoped<IKeycloakManager, KeycloakManager>();

        return services;
    }
}
