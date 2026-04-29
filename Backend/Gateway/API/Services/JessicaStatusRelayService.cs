using Gateway.API.DTOs.Responses;
using Gateway.API.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Gateway.API.Services;

public sealed class JessicaStatusRelayService(
    ILogger<JessicaStatusRelayService> logger,
    IHttpClientFactory httpClientFactory,
    IHubContext<JessicaHub> hubContext) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(250);
    private static readonly TimeSpan FailurePollInterval = TimeSpan.FromSeconds(3);

    private readonly ILogger<JessicaStatusRelayService> _logger = logger;
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly IHubContext<JessicaHub> _hubContext = hubContext;
    private DateTime? _lastForwardedAtUtc;
    private int _pollFailureCount;
    private DateTime _lastDetailedFailureLogUtc = DateTime.MinValue;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("📡 Jessica status relay started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("JessicaManager");
                var status = await client
                    .GetFromJsonAsync<RobotStatusResponse>("/api/car/status", stoppingToken)
                    .ConfigureAwait(false);

                if (status is null || !status.Available)
                {
                    await Task.Delay(PollInterval, stoppingToken).ConfigureAwait(false);
                    continue;
                }

                _pollFailureCount = 0;

                if (_lastForwardedAtUtc.HasValue && status.ReceivedAtUtc <= _lastForwardedAtUtc.Value)
                {
                    await Task.Delay(PollInterval, stoppingToken).ConfigureAwait(false);
                    continue;
                }

                _lastForwardedAtUtc = status.ReceivedAtUtc;

                await _hubContext.Clients.All.SendAsync("RobotStatusUpdated", new
                {
                    distance = status.Distance,
                    safety = status.Safety,
                    mode = status.Mode,
                    battery = status.Battery,
                    receivedAtUtc = status.ReceivedAtUtc
                }, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _pollFailureCount++;
                var nowUtc = DateTime.UtcNow;
                var shouldLogDetailed = _pollFailureCount <= 2 ||
                                        _pollFailureCount % 20 == 0 ||
                                        nowUtc - _lastDetailedFailureLogUtc > TimeSpan.FromMinutes(1);

                if (shouldLogDetailed)
                {
                    _lastDetailedFailureLogUtc = nowUtc;
                    _logger.LogWarning(
                        ex,
                        "Jessica status relay poll failed. Slowing retry. FailureCount={FailureCount}",
                        _pollFailureCount);
                }
                else
                {
                    _logger.LogDebug(
                        "Jessica status relay transient failure. FailureCount={FailureCount}",
                        _pollFailureCount);
                }

                await Task.Delay(FailurePollInterval, stoppingToken).ConfigureAwait(false);
                continue;
            }

            await Task.Delay(PollInterval, stoppingToken).ConfigureAwait(false);
        }

        _logger.LogInformation("📡 Jessica status relay stopped.");
    }
}
