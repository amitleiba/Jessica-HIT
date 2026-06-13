using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using MetricsService.Domain.Entities;
using MetricsService.Infrastructure.Persistence;

namespace MetricsService.Infrastructure.Services;

/// <summary>
/// Background worker that periodically polls JessicaManager for the latest robot status
/// and stores new telemetry metrics in the database.
/// </summary>
public sealed class MetricsCollectorWorker(
    ILogger<MetricsCollectorWorker> logger,
    IHttpClientFactory httpClientFactory,
    IServiceProvider serviceProvider) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);

    private readonly ILogger<MetricsCollectorWorker> _logger = logger;
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    private DateTime? _lastSavedTimestampUtc;
    private int _consecutiveFailures;
    private DateTime _lastDetailedFailureLogUtc = DateTime.MinValue;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🚀 Metrics Collector Worker started.");

        using var timer = new PeriodicTimer(PollInterval);

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
        {
            try
            {
                var client = _httpClientFactory.CreateClient("JessicaManager");
                
                // Using the internal status relay header (same as Gateway status relay) to bypass strict user auth on internal calls
                using var request = new HttpRequestMessage(HttpMethod.Get, "/api/car/status");
                request.Headers.TryAddWithoutValidation("X-User-Id", "internal-status-relay");

                using var response = await client.SendAsync(request, stoppingToken).ConfigureAwait(false);

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    _logger.LogDebug("Jessica status not found (disconnected or not started).");
                    _consecutiveFailures = 0; // Handled state
                    continue;
                }

                response.EnsureSuccessStatusCode();

                var status = await response.Content
                    .ReadFromJsonAsync<RobotStatusResponse>(cancellationToken: stoppingToken)
                    .ConfigureAwait(false);

                if (status is null || !status.Available)
                {
                    continue;
                }

                _consecutiveFailures = 0;

                // Deduplicate: only save if this is a newer reading
                if (_lastSavedTimestampUtc.HasValue && status.ReceivedAtUtc <= _lastSavedTimestampUtc.Value)
                {
                    continue;
                }

                // Retrieve the latest saved timestamp from the database on first run/fallback
                if (!_lastSavedTimestampUtc.HasValue)
                {
                    _lastSavedTimestampUtc = await GetLatestSavedTimestampAsync(stoppingToken).ConfigureAwait(false);
                    if (_lastSavedTimestampUtc.HasValue && status.ReceivedAtUtc <= _lastSavedTimestampUtc.Value)
                    {
                        // Sample is stale relative to DB; skip without moving the watermark backwards
                        continue;
                    }
                }

                // Create a new metric entry
                var metric = new SensorMetric
                {
                    Distance = status.Distance,
                    Safety = status.Safety,
                    Mode = status.Mode,
                    SolarVoltage = status.Battery, // Maps the robot's voltage/battery level to solar voltage
                    Timestamp = status.ReceivedAtUtc,
                    SavedAt = DateTime.UtcNow
                };

                await SaveMetricAsync(metric, stoppingToken).ConfigureAwait(false);
                
                _lastSavedTimestampUtc = status.ReceivedAtUtc;

                _logger.LogDebug(
                    "💾 Saved telemetry: Distance={Distance}, Safety={Safety}, Mode={Mode}, SolarVoltage={SolarVoltage}V, RobotTime={RobotTime}",
                    metric.Distance,
                    metric.Safety,
                    metric.Mode,
                    metric.SolarVoltage,
                    metric.Timestamp);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _consecutiveFailures++;
                var nowUtc = DateTime.UtcNow;
                var shouldLogDetailed = _consecutiveFailures <= 2 ||
                                        _consecutiveFailures % 30 == 0 ||
                                        nowUtc - _lastDetailedFailureLogUtc > TimeSpan.FromMinutes(2);

                if (shouldLogDetailed)
                {
                    _lastDetailedFailureLogUtc = nowUtc;
                    _logger.LogWarning(
                        ex,
                        "⚠ Failed to poll JessicaManager status. ConsecutiveFailures={Failures}",
                        _consecutiveFailures);
                }
                else
                {
                    _logger.LogDebug("Transient metrics poll failure. ConsecutiveFailures={Failures}", _consecutiveFailures);
                }
            }
        }

        _logger.LogInformation("🛑 Metrics Collector Worker stopped.");
    }

    private async Task SaveMetricAsync(SensorMetric metric, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MetricsDbContext>();

        context.SensorMetrics.Add(metric);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<DateTime?> GetLatestSavedTimestampAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MetricsDbContext>();

        var latest = await context.SensorMetrics
            .OrderByDescending(m => m.Timestamp)
            .Select(m => (DateTime?)m.Timestamp)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return latest;
    }

    private sealed class RobotStatusResponse
    {
        public bool Available { get; set; }
        public int Distance { get; set; }
        public int Safety { get; set; }
        public int Mode { get; set; }
        public double Battery { get; set; }
        public DateTime ReceivedAtUtc { get; set; }
    }
}
