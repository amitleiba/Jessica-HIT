using Gateway.Domain.Configuration;
using Microsoft.OpenApi.Models;

namespace Gateway.Extensions;

public static class SwaggerExtensions
{
    /// <summary>
    /// Configures Swagger/OpenAPI with Keycloak OAuth2 authentication
    /// </summary>
    public static IServiceCollection AddSwaggerWithKeycloak(
        this IServiceCollection services,
        SwaggerConfig swaggerConfig,
        KeycloakConfig keycloakConfig,
        ILogger logger)
    {
        if (!swaggerConfig.Enabled)
        {
            return services;
        }

        logger.LogInformation("Configuring Swagger with Keycloak OAuth2");

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            // Fix for nested class names (e.g., Request+DTO becomes Request-DTO)
            options.CustomSchemaIds(id => id.FullName!.Replace('+', '-'));

            // Swagger document configuration
            options.SwaggerDoc(swaggerConfig.Version, new OpenApiInfo
            {
                Title = swaggerConfig.Title,
                Version = swaggerConfig.Version,
                Description = swaggerConfig.Description
            });

            // Configure OAuth2 with Keycloak
            options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.OAuth2,
                Flows = new OpenApiOAuthFlows
                {
                    AuthorizationCode = new OpenApiOAuthFlow
                    {
                        AuthorizationUrl = new Uri($"{keycloakConfig.Authority}/protocol/openid-connect/auth"),
                        TokenUrl = new Uri($"{keycloakConfig.Authority}/protocol/openid-connect/token"),
                        Scopes = new Dictionary<string, string>
                        {
                            { "openid", "OpenID Connect scope" },
                            { "profile", "User profile information" },
                            { "email", "User email address" }
                        }
                    }
                }
            });

            // Require OAuth2 for all operations
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "oauth2"
                        },
                        Scheme = "Bearer",
                        In = ParameterLocation.Header,
                        Name = "Authorization"
                    },
                    new[] { "openid", "profile", "email" }
                }
            });
        });

        logger.LogInformation("Swagger configured successfully");

        return services;
    }

    /// <summary>
    /// Configures Swagger UI middleware with Keycloak OAuth2
    /// </summary>
    public static IApplicationBuilder UseSwaggerWithKeycloak(
        this IApplicationBuilder app,
        SwaggerConfig swaggerConfig,
        KeycloakConfig keycloakConfig,
        ILogger logger)
    {
        if (!swaggerConfig.Enabled)
        {
            return app;
        }

        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint(
                $"/swagger/{swaggerConfig.Version}/swagger.json",
                $"{swaggerConfig.Title} {swaggerConfig.Version}");
            options.RoutePrefix = "swagger"; // Access at /swagger

            // Configure OAuth2 for Swagger UI
            options.OAuthClientId(keycloakConfig.ClientId);
            options.OAuthClientSecret(keycloakConfig.ClientSecret);
            options.OAuthUsePkce();
            options.OAuthScopes("openid", "profile", "email");
            options.OAuthAppName("Jessica Gateway API");
        });

        logger.LogInformation("Swagger UI available at: http://localhost:5207/swagger");

        return app;
    }
}
