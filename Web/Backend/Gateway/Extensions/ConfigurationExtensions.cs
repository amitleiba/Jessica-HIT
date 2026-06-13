using Gateway.Domain.Configuration;

namespace Gateway.Extensions;

public static class ConfigurationExtensions
{
    /// <summary>
    /// Configures and validates all application configuration options.
    /// </summary>
    public static IServiceCollection AddConfigurationOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtConfig>()
            .Bind(configuration.GetSection(JwtConfig.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<ReverseProxyConfig>()
            .Bind(configuration.GetSection(ReverseProxyConfig.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<SwaggerConfig>()
            .Bind(configuration.GetSection(SwaggerConfig.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<CorsConfig>()
            .Bind(configuration.GetSection(CorsConfig.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }

    /// <summary>
    /// Gets JWT configuration from settings.
    /// </summary>
    public static JwtConfig GetJwtConfig(this IConfiguration configuration)
    {
        return configuration.GetSection(JwtConfig.SectionName).Get<JwtConfig>()
            ?? throw new InvalidOperationException("JWT configuration is missing or invalid");
    }

    /// <summary>
    /// Gets Swagger configuration from settings with fallback to defaults.
    /// </summary>
    public static SwaggerConfig GetSwaggerConfig(this IConfiguration configuration)
    {
        return configuration.GetSection(SwaggerConfig.SectionName).Get<SwaggerConfig>()
            ?? new SwaggerConfig();
    }

    /// <summary>
    /// Gets CORS configuration from settings.
    /// </summary>
    public static CorsConfig GetCorsConfig(this IConfiguration configuration)
    {
        return configuration.GetSection(CorsConfig.SectionName).Get<CorsConfig>()
            ?? throw new InvalidOperationException("CORS configuration is missing or invalid");
    }
}
