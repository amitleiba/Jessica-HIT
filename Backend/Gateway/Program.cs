using Gateway.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var logger = LoggerFactory.Create(config => config.AddConsole()).CreateLogger("Gateway.Startup");

builder.Services.AddConfigurationOptions(builder.Configuration);

var keycloakConfig = builder.Configuration.GetKeycloakConfig();
var swaggerConfig = builder.Configuration.GetSwaggerConfig();
var corsConfig = builder.Configuration.GetCorsConfig();

// Clean Architecture - Register Application and Infrastructure layers
builder.Services.AddApplicationServices(builder.Configuration);

builder.Services.AddKeycloakAuthentication(keycloakConfig, logger);
builder.Services.AddAuthorizationPolicies();
builder.Services.AddControllers();

// Add CORS for SignalR
builder.Services.AddCors(options =>
{
    options.AddPolicy("SignalRCorsPolicy", policy =>
    {
        if (corsConfig.AllowAnyHeader)
        {
            policy.AllowAnyHeader();
        }

        if (corsConfig.AllowAnyMethod)
        {
            policy.AllowAnyMethod();
        }

        if (corsConfig.AllowCredentials)
        {
            policy.AllowCredentials();
        }

        policy.WithOrigins(corsConfig.AllowedOrigins);
    });
});

builder.Services.AddSignalR();  // Add SignalR
builder.Services.AddSwaggerWithKeycloak(swaggerConfig, keycloakConfig, logger);
builder.Services.AddReverseProxyWithUserForwarding(builder.Configuration);

var app = builder.Build();

app.UseSwaggerWithKeycloak(swaggerConfig, keycloakConfig, logger);
app.UseCors("SignalRCorsPolicy");  // Enable CORS for SignalR
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<Gateway.API.Hubs.JessicaHub>("/hubs/jessica");  // Map SignalR hub
app.MapReverseProxy();
app.MapDefaultEndpoints();

app.Run();
