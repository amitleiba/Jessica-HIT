using System.Net;
using System.Net.Http.Json;
using Gateway.API.DTOs.Responses;
using Gateway.API.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Gateway.API.Services;

public sealed class JessicaStatusRelayService(
    ILogger<JessicaStatusRelayService> logger,
    IHttpClientFactory httpClientFactory,
    IHubContext<JessicaHub> hubContext,
    IConfiguration configuration) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(250);
    private static readonly TimeSpan FailurePollInterval = TimeSpan.FromSeconds(3);

    private readonly ILogger<JessicaStatusRelayService> _logger = logger;
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly IHubContext<JessicaHub> _hubContext = hubContext;
    private readonly IConfiguration _configuration = configuration;
    private DateTime? _lastForwardedAtUtc;
    private int _pollFailureCount;
    private DateTime _lastDetailedFailureLogUtc = DateTime.MinValue;
    private bool? _lastForwardedAvailable;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("📡 Jessica status relay started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var relayUserId = _configuration["JessicaManager:StatusRelayUserId"];
                if (string.IsNullOrWhiteSpace(relayUserId))
                {
                    relayUserId = "internal-status-relay";
                }

                var client = _httpClientFactory.CreateClient("JessicaManager");
                using var request = new HttpRequestMessage(HttpMethod.Get, "/api/car/status");
                request.Headers.TryAddWithoutValidation("X-User-Id", relayUserId);

                var response = await client.SendAsync(request, stoppingToken).ConfigureAwait(false);

                RobotStatusResponse? status;
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    status = new RobotStatusResponse { Available = false };
                }
                else
                {
                    response.EnsureSuccessStatusCode();
                    status = await response.Content
                        .ReadFromJsonAsync<RobotStatusResponse>(cancellationToken: stoppingToken)
                        .ConfigureAwait(false);
                }

                if (status is null || !status.Available)
                {
                    if (!_lastForwardedAvailable.HasValue || _lastForwardedAvailable.Value)
                    {
                        _lastForwardedAvailable = false;
                        await _hubContext.Clients.All.SendAsync("RobotStatusUpdated", new
                        {
                            available = false,
                            distance = 0,
                            safety = 0,
                            mode = 0,
                            battery = 0.0,
                            receivedAtUtc = DateTime.UtcNow
                        }, stoppingToken).ConfigureAwait(false);
                    }

                    await Task.Delay(PollInterval, stoppingToken).ConfigureAwait(false);
                    continue;
                }

                _pollFailureCount = 0;

                var isTransition = !_lastForwardedAvailable.HasValue || !_lastForwardedAvailable.Value;
                if (!isTransition && _lastForwardedAtUtc.HasValue && status.ReceivedAtUtc <= _lastForwardedAtUtc.Value)
                {
                    await Task.Delay(PollInterval, stoppingToken).ConfigureAwait(false);
                    continue;
                }

                _lastForwardedAtUtc = status.ReceivedAtUtc;
                _lastForwardedAvailable = true;

                await _hubContext.Clients.All.SendAsync("RobotStatusUpdated", new
                {
                    available = true,
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
