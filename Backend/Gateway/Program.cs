using Gateway.Extensions;

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
builder.Services.AddSwaggerWithJwt(swaggerConfig, logger);
builder.Services.AddReverseProxyWithUserForwarding(builder.Configuration);

var app = builder.Build();

app.UseSwaggerWithJwt(swaggerConfig, logger);
app.UseCors("SignalRCorsPolicy");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<Gateway.API.Hubs.JessicaHub>("/hubs/jessica");
app.MapReverseProxy();
app.MapDefaultEndpoints();

app.Run();
