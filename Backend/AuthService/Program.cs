using AuthService.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var logger = LoggerFactory.Create(config => config.AddConsole()).CreateLogger("AuthService.Startup");

// ── Configuration ──
builder.Services.AddConfigurationOptions(builder.Configuration);

var jwtConfig = builder.Configuration.GetJwtConfig();
var swaggerConfig = builder.Configuration.GetSwaggerConfig();

// ── Database (PostgreSQL via EF Core) ──
builder.Services.AddAuthDatabase(builder.Configuration);

// ── Clean Architecture — Register Application and Infrastructure layers ──
builder.Services.AddApplicationServices(builder.Configuration);

// ── Authentication (JWT Bearer — validates its own tokens) ──
builder.Services.AddJwtAuthentication(jwtConfig, logger);

// ── Controllers + Swagger ──
builder.Services.AddControllers();
builder.Services.AddSwaggerWithJwt(swaggerConfig, logger);

// ── CORS (for direct frontend access during development) ──
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:4200", "http://localhost:17163")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

// ── Database initialization (migrate + seed roles) ──
await app.InitializeDatabaseAsync().ConfigureAwait(false);

// ── Middleware pipeline ──
app.UseSwaggerWithJwt(swaggerConfig, logger);
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapDefaultEndpoints();

logger.LogInformation("AuthService started successfully");

await app.RunAsync().ConfigureAwait(false);

