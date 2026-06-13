using System.Text;
using MetricsService.Extensions;
using MetricsService.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Register Aspire Service Defaults (Telemetry, Service Discovery, Health Checks)
builder.AddServiceDefaults();

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// ── Database Setup ──
builder.Services.AddMetricsDatabase(builder.Configuration);

// ── Background Collector Worker ──
builder.Services.AddHttpClient("JessicaManager", (sp, client) =>
{
    var baseUrl = sp.GetRequiredService<IConfiguration>()["JessicaManager:BaseUrl"];
    if (string.IsNullOrWhiteSpace(baseUrl))
    {
        baseUrl = "http://jessicamanager";
    }

    client.BaseAddress = new Uri(baseUrl);
});

builder.Services.AddHostedService<MetricsCollectorWorker>();

// ── JWT Bearer Authentication ──
var jwtSection = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSection["SecretKey"] ?? throw new InvalidOperationException("Jwt:SecretKey is missing from configuration");
var issuer    = jwtSection["Issuer"]    ?? throw new InvalidOperationException("Jwt:Issuer is missing from configuration");
var audience  = jwtSection["Audience"]  ?? throw new InvalidOperationException("Jwt:Audience is missing from configuration");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidIssuer              = issuer,
            ValidateAudience         = true,
            ValidAudience            = audience,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            NameClaimType            = "preferred_username",
            RoleClaimType            = "role",
            ClockSkew                = TimeSpan.FromMinutes(2)
        };
    });

builder.Services.AddAuthorization();

// ── Controllers and API docs ──
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Jessica Metrics API", Version = "v1" });
});

var app = builder.Build();

// Obtain logger from DI (honors configured log filters/enrichers)
var logger = app.Services.GetRequiredService<ILogger<Program>>();

// ── Database Initialization ──
await app.InitializeDatabaseAsync();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Jessica Metrics API v1");
    });
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapDefaultEndpoints();

logger.LogInformation("MetricsService started successfully");

await app.RunAsync();

public partial class Program;
