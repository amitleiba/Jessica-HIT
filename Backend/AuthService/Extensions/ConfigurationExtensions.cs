using AuthService.Domain.Configuration;

namespace AuthService.Extensions;

/// <summary>
/// Extension methods for binding and retrieving configuration sections.
/// </summary>
public static class ConfigurationExtensions
{
    /// <summary>
    /// Registers and validates all configuration options from appsettings.json.
    /// </summary>
    public static IServiceCollection AddConfigurationOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtConfig>()
            .Bind(configuration.GetSection(JwtConfig.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<CryptoConfig>()
            .Bind(configuration.GetSection(CryptoConfig.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<SwaggerConfig>()
            .Bind(configuration.GetSection(SwaggerConfig.SectionName))
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
    /// Gets Crypto configuration from settings.
    /// </summary>
    public static CryptoConfig GetCryptoConfig(this IConfiguration configuration)
    {
        return configuration.GetSection(CryptoConfig.SectionName).Get<CryptoConfig>()
            ?? throw new InvalidOperationException("Crypto configuration is missing or invalid");
    }

    /// <summary>
    /// Gets Swagger configuration from settings with fallback to defaults.
    /// </summary>
    public static SwaggerConfig GetSwaggerConfig(this IConfiguration configuration)
    {
        return configuration.GetSection(SwaggerConfig.SectionName).Get<SwaggerConfig>()
            ?? new SwaggerConfig();
    }
}

