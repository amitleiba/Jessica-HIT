using RecordingManager.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var logger = LoggerFactory.Create(config => config.AddConsole()).CreateLogger("RecordingManager.Startup");

// ── Database (PostgreSQL via EF Core) ──
builder.Services.AddRecordingDatabase(builder.Configuration);

// ── Clean Architecture — Register Application and Infrastructure layers ──
builder.Services.AddApplicationServices();

// ── Controllers + Swagger ──
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ── Database initialization (migrate or EnsureCreated) ──
await app.InitializeDatabaseAsync().ConfigureAwait(false);

// ── Middleware pipeline ──
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();
app.MapDefaultEndpoints();

logger.LogInformation("RecordingManager started successfully");

await app.RunAsync().ConfigureAwait(false);

