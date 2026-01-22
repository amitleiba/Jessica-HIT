using Gateway.Domain.Configuration;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Gateway.Extensions;

public static class AuthenticationExtensions
{
    /// <summary>
    /// Configures Keycloak authentication with Cookie, OpenID Connect, and JWT Bearer
    /// </summary>
    public static IServiceCollection AddKeycloakAuthentication(
        this IServiceCollection services,
        KeycloakConfig keycloakConfig,
        ILogger logger)
    {
        services.AddAuthentication(options =>
        {
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
        })
        .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
        {
            options.Cookie.Name = "JessicaAuth";
            options.Cookie.SameSite = SameSiteMode.Lax;
        })
        .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            options.Authority = keycloakConfig.Authority;
            options.RequireHttpsMetadata = keycloakConfig.RequireHttpsMetadata;
            options.MetadataAddress = $"{keycloakConfig.Authority}/.well-known/openid-configuration";
            
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = keycloakConfig.Authority,
                ValidateAudience = false, // Keycloak tokens often have "account" as audience
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                NameClaimType = "preferred_username",
                RoleClaimType = "roles",
                ClockSkew = TimeSpan.FromMinutes(5)
            };
            
            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    logger.LogError("JWT Bearer authentication failed: {Error} | {Detail}", 
                        context.Exception.Message, 
                        context.Exception.InnerException?.Message ?? "No inner exception");
                    return Task.CompletedTask;
                },
                OnTokenValidated = context =>
                {
                    var username = context.Principal?.FindFirst("preferred_username")?.Value ?? "Unknown";
                    logger.LogInformation("JWT token validated successfully for user: {User}", username);
                    return Task.CompletedTask;
                },
                OnMessageReceived = context =>
                {
                    var authHeader = context.Request.Headers["Authorization"].ToString();
                    logger.LogInformation("JWT Bearer: Received token, Header present: {HasToken}", 
                        !string.IsNullOrEmpty(authHeader));
                    return Task.CompletedTask;
                },
                OnChallenge = context =>
                {
                    logger.LogWarning("JWT Bearer challenge triggered. Error: {Error}, ErrorDescription: {ErrorDesc}", 
                        context.Error ?? "none", 
                        context.ErrorDescription ?? "none");
                    return Task.CompletedTask;
                }
            };
        })
        .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
        {
            options.Authority = keycloakConfig.Authority;
            options.ClientId = keycloakConfig.ClientId;
            options.ClientSecret = keycloakConfig.ClientSecret;
            options.ResponseType = OpenIdConnectResponseType.Code;
            options.SaveTokens = keycloakConfig.SaveTokens;
            options.GetClaimsFromUserInfoEndpoint = keycloakConfig.GetClaimsFromUserInfoEndpoint;
            options.RequireHttpsMetadata = keycloakConfig.RequireHttpsMetadata;

            options.Scope.Clear();
            foreach (var scope in keycloakConfig.Scope)
            {
                options.Scope.Add(scope);
            }

            options.TokenValidationParameters = new TokenValidationParameters
            {
                NameClaimType = "preferred_username",
                RoleClaimType = "roles",
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true
            };

            options.Events = new OpenIdConnectEvents
            {
                OnAuthenticationFailed = context =>
                {
                    logger.LogError("Authentication failed: {Error}", context.Exception.Message);
                    return Task.CompletedTask;
                }
            };
        });

        return services;
    }

    /// <summary>
    /// Configures authorization policies
    /// </summary>
    public static IServiceCollection AddAuthorizationPolicies(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy("authenticated", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.AddAuthenticationSchemes(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    JwtBearerDefaults.AuthenticationScheme,
                    OpenIdConnectDefaults.AuthenticationScheme
                );
            });
        });

        return services;
    }
}
