using MetricsService.Extensions;
using MetricsService.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Register Aspire Service Defaults (Telemetry, Service Discovery, Health Checks)
builder.AddServiceDefaults();

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var logger = LoggerFactory.Create(config => config.AddConsole()).CreateLogger("MetricsService.Startup");

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
    logger.LogInformation("JessicaManager HTTP Client configured at base URL: {BaseUrl}", baseUrl);
});

builder.Services.AddHostedService<MetricsCollectorWorker>();

// ── Controllers and API docs ──
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Jessica Metrics API", Version = "v1" });
});

var app = builder.Build();

// ── Database Initialization ──
await app.InitializeDatabaseAsync().ConfigureAwait(false);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Jessica Metrics API v1");
    });
}

app.UseAuthorization();
app.MapControllers();
app.MapDefaultEndpoints();

logger.LogInformation("MetricsService started successfully");

await app.RunAsync().ConfigureAwait(false);

public partial class Program;
