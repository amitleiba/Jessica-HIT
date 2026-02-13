namespace Gateway.Extensions;

/// <summary>
/// Extension methods for registering services with dependency injection.
/// Simplified after removing Keycloak — auth is handled by AuthService microservice.
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Registers all application services.
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Register HttpContextAccessor (required by middleware and hubs)
        services.AddHttpContextAccessor();

        // Register configuration instances (for direct injection)
        services.AddSingleton(sp => configuration.GetJwtConfig());

        return services;
    }
}
