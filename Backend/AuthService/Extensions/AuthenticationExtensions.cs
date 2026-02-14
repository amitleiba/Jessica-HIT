using AuthService.Domain.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace AuthService.Extensions;

/// <summary>
/// Configures JWT Bearer authentication for the AuthService itself.
/// This allows the AuthService to protect its own endpoints (user-info, logout)
/// using the same JWT tokens it generates.
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
                        logger.LogError("JWT authentication failed: {Error}", context.Exception.Message);
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        var username = context.Principal?.FindFirst("preferred_username")?.Value ?? "Unknown";
                        logger.LogInformation("JWT validated for user: {User}", username);
                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization();

        logger.LogInformation("JWT Bearer authentication configured successfully");

        return services;
    }
}