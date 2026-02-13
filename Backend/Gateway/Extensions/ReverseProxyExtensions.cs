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
                    var username = transformContext.HttpContext.User?.Identity?.Name;
                    if (!string.IsNullOrEmpty(username))
                    {
                        transformContext.ProxyRequest.Headers.Add("X-Forwarded-User", username);
                    }

                    return ValueTask.CompletedTask;
                });
            });

        return services;
    }
}
