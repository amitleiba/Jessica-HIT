using System.Security.Claims;
using Yarp.ReverseProxy.Transforms;

namespace Gateway.Extensions;

public static class ReverseProxyExtensions
{
    /// <summary>
    /// Configures YARP reverse proxy with user identity forwarding
    /// </summary>
    public static IServiceCollection AddReverseProxyWithUserForwarding(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddReverseProxy()
            .LoadFromConfig(configuration.GetSection("ReverseProxy"))
            .AddServiceDiscoveryDestinationResolver()   // Aspire service discovery — resolves http://authservice → localhost:random_port
            .AddTransforms(builderContext =>
            {
                builderContext.AddRequestTransform(transformContext =>
                {
                    var user = transformContext.HttpContext.User;

                    var username = user?.Identity?.Name;
                    if (!string.IsNullOrEmpty(username))
                    {
                        transformContext.ProxyRequest.Headers.Add("X-Forwarded-User", username);
                    }

                    // Forward the user's unique ID for per-user data scoping.
                    // The JWT "sub" claim may be mapped to ClaimTypes.NameIdentifier by .NET's
                    // JWT handler, so we check both the raw "sub" and the mapped long-form URI.
                    var userId = user?.FindFirst("sub")?.Value
                              ?? user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                    if (!string.IsNullOrEmpty(userId))
                    {
                        transformContext.ProxyRequest.Headers.Add("X-User-Id", userId);
                    }

                    return ValueTask.CompletedTask;
                });
            });

        return services;
    }
}
