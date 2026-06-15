using Gateway.Domain.Configuration;
using Microsoft.OpenApi.Models;

namespace Gateway.Extensions;

/// <summary>
/// Configures Swagger/OpenAPI with JWT Bearer authentication.
/// Simplified after removing Keycloak OAuth2 setup.
/// </summary>
public static class SwaggerExtensions
{
    public static IServiceCollection AddSwaggerWithJwt(
        this IServiceCollection services,
        SwaggerConfig swaggerConfig,
        ILogger logger)
    {
        if (!swaggerConfig.Enabled)
        {
            return services;
        }

        logger.LogInformation("Configuring Swagger with JWT Bearer auth");

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.CustomSchemaIds(id => id.FullName!.Replace('+', '-'));

            options.SwaggerDoc(swaggerConfig.Version, new OpenApiInfo
            {
                Title = swaggerConfig.Title,
                Version = swaggerConfig.Version,
                Description = swaggerConfig.Description
            });

            // Configure JWT Bearer auth for Swagger UI
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Enter your JWT access token"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        logger.LogInformation("Swagger configured successfully");

        return services;
    }

    public static IApplicationBuilder UseSwaggerWithJwt(
        this IApplicationBuilder app,
        SwaggerConfig swaggerConfig,
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
            options.RoutePrefix = "swagger";
        });

        logger.LogInformation("Swagger UI available at: http://localhost:5207/swagger");

        return app;
    }
}
