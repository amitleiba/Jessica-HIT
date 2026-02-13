using System.Text;
using Gateway.Domain.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Gateway.Extensions;

/// <summary>
/// Configures JWT Bearer authentication for the Gateway.
/// Validates tokens issued by the AuthService using the shared HMAC-SHA256 key.
/// Replaces the previous Keycloak authentication setup.
/// </summary>
public static class AuthenticationExtensions
{
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        JwtConfig jwtConfig,
        ILogger logger)
    {
        logger.LogInformation("Configuring JWT Bearer authentication (Issuer: {Issuer}, Audience: {Audience})",
            jwtConfig.Issuer, jwtConfig.Audience);

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtConfig.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtConfig.Audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtConfig.SecretKey)),
                    NameClaimType = "preferred_username",
                    RoleClaimType = "role",
                    ClockSkew = TimeSpan.FromMinutes(2)
                };

                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        logger.LogError("JWT authentication failed: {Error} | {Detail}",
                            context.Exception.Message,
                            context.Exception.InnerException?.Message ?? "No inner exception");
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        var username = context.Principal?.FindFirst("preferred_username")?.Value ?? "Unknown";
                        logger.LogInformation("JWT validated for user: {User}", username);
                        return Task.CompletedTask;
                    },
                    OnMessageReceived = context =>
                    {
                        var authHeader = context.Request.Headers["Authorization"].ToString();
                        logger.LogDebug("JWT Bearer: Header present: {HasToken}", !string.IsNullOrEmpty(authHeader));
                        return Task.CompletedTask;
                    }
                };
            });

        logger.LogInformation("JWT Bearer authentication configured successfully");

        return services;
    }

    /// <summary>
    /// Configures authorization policies for YARP routes and controllers.
    /// </summary>
    public static IServiceCollection AddAuthorizationPolicies(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy("authenticated", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme);
            });
        });

        return services;
    }
}
