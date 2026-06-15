using Microsoft.Extensions.Hosting;
using JessicaManager.Application.Adapters;
using JessicaManager.Application.Services;
using JessicaManager.Infrastructure.Options;
using JessicaManager.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Configuration ────────────────────────────────────────────────────────────
// Add the persisted Gateway IP file to the config pipeline.
// reloadOnChange:true means IOptionsMonitor<JessicaWebSocketOptions> will fire
// its OnChange event automatically the moment GatewayIpPersistenceService writes
// to this file — zero manual wiring required.
builder.Configuration.AddJsonFile(
    "gateway-ip.json",
    optional: true,
    reloadOnChange: true);

builder.AddServiceDefaults();

// ── Services ─────────────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IMovementConversionService, MovementConversionService>();
builder.Services
    .AddOptions<JessicaWebSocketOptions>()
    .Bind(builder.Configuration.GetSection(JessicaWebSocketOptions.SectionName))
    .Validate(
        options => options.Url is not null && (options.Url.Scheme == "ws" || options.Url.Scheme == "wss"),
        $"{JessicaWebSocketOptions.SectionName}:Url must be an absolute ws/wss URL.");
builder.Services.AddSingleton<ICurrentSpeedState, InMemoryCurrentSpeedState>();
builder.Services.AddSingleton<IRobotStatusState, InMemoryRobotStatusState>();
builder.Services.AddSingleton<GatewayIpPersistenceService>();
builder.Services.AddSingleton<JessicaWebSocketMoveCommandPublisher>();
builder.Services.AddSingleton<IMoveCommandPublisher>(sp => sp.GetRequiredService<JessicaWebSocketMoveCommandPublisher>());
builder.Services.AddSingleton<IConnectionManager>(sp => sp.GetRequiredService<JessicaWebSocketMoveCommandPublisher>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<JessicaWebSocketMoveCommandPublisher>());

var app = builder.Build();
var wsOptions = app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptionsMonitor<JessicaWebSocketOptions>>().CurrentValue;
app.Logger.LogInformation("Jessica WS target configured: {Url}", wsOptions.Url);

// ── HTTP Pipeline ─────────────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.MapDefaultEndpoints();

app.Run();

public partial class Program;
