using Gateway.Extensions;
using Gateway.API.Services;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var logger = LoggerFactory.Create(config => config.AddConsole()).CreateLogger("Gateway.Startup");

builder.Services.AddConfigurationOptions(builder.Configuration);

var jwtConfig = builder.Configuration.GetJwtConfig();
var swaggerConfig = builder.Configuration.GetSwaggerConfig();
var corsConfig = builder.Configuration.GetCorsConfig();

// Clean Architecture - Register Application services
builder.Services.AddApplicationServices(builder.Configuration);

// JWT Bearer authentication (validates tokens from AuthService)
builder.Services.AddJwtAuthentication(jwtConfig, logger);
builder.Services.AddAuthorizationPolicies();
builder.Services.AddControllers();

// CORS for SignalR and frontend
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

builder.Services.AddSignalR();
builder.Services.AddHttpClient("JessicaManager", (sp, client) =>
{
    var baseUrl = sp.GetRequiredService<IConfiguration>()["JessicaManager:BaseUrl"];
    if (string.IsNullOrWhiteSpace(baseUrl))
    {
        baseUrl = "http://jessicamanager";
    }

    client.BaseAddress = new Uri(baseUrl);
    logger.LogInformation("JessicaManager HTTP client BaseAddress={BaseAddress}", baseUrl);
});
builder.Services.AddHostedService<JessicaStatusRelayService>();
builder.Services.AddSwaggerWithJwt(swaggerConfig, logger);
builder.Services.AddReverseProxyWithUserForwarding(builder.Configuration);

// Add Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("AuthLimiter", opt =>
    {
        opt.PermitLimit = 10;
        opt.Window = TimeSpan.FromSeconds(10);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 0;
    });
});

var app = builder.Build();

app.UseSwaggerWithJwt(swaggerConfig, logger);
app.UseCors("SignalRCorsPolicy");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<Gateway.API.Hubs.JessicaHub>("/hubs/jessica");
app.MapReverseProxy();
app.MapDefaultEndpoints();

app.Run();
