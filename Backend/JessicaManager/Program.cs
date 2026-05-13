using Microsoft.Extensions.Hosting;
using JessicaManager.Application.Adapters;
using JessicaManager.Application.Services;
using JessicaManager.Infrastructure.Options;
using JessicaManager.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container
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
builder.Services.AddSingleton<JessicaWebSocketMoveCommandPublisher>();
builder.Services.AddSingleton<IMoveCommandPublisher>(sp => sp.GetRequiredService<JessicaWebSocketMoveCommandPublisher>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<JessicaWebSocketMoveCommandPublisher>());

var app = builder.Build();
var wsOptions = app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<JessicaWebSocketOptions>>().Value;
app.Logger.LogInformation("Jessica WS target configured: {Url}", wsOptions.Url);

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.MapDefaultEndpoints();

app.Run();

public partial class Program;

