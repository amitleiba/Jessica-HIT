using Microsoft.Extensions.Hosting;
using Yarp.ReverseProxy.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// YARP Reverse Proxy setup (no routes configured yet)
// Add an in-memory configuration provider with empty config
builder.Services.AddReverseProxy()
    .LoadFromMemory(new List<RouteConfig>(), new List<ClusterConfig>());

var app = builder.Build();

app.MapReverseProxy();
app.MapDefaultEndpoints();

app.Run();
