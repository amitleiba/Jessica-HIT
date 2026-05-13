using System.Net.Http.Json;
using Gateway.API.DTOs.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Gateway.API.Hubs;

/// <summary>
/// SignalR Hub for real-time communication with Jessica drones.
/// Requires authentication via Keycloak JWT token.
///
/// Client → Server methods:
///   CarDirectionChange(CarDirectionRequest)  — user changed car direction
///   CarStart()                                — user started the car
///   CarStop()                                 — user stopped the car
/// </summary>
[Authorize]
public class JessicaHub(ILogger<JessicaHub> logger, IHttpClientFactory httpClientFactory) : Hub
{
    private readonly ILogger<JessicaHub> _logger = logger;
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

    /// <summary>
    /// Called when a client connects to the hub
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("User {ConnectionId} connected", Context.ConnectionId);
        await base.OnConnectedAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Called when a client disconnects from the hub
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("User {ConnectionId} disconnected. Reason: {Reason}",
            Context.ConnectionId, exception?.Message ?? "clean");
        await base.OnDisconnectedAsync(exception).ConfigureAwait(false);
    }

    // ──────────────────────────────────────────────────
    //  Car control methods (Client → Server)
    // ──────────────────────────────────────────────────

    /// <summary>
    /// Receives a car direction change from the frontend control panel.
    /// Only called when the car is running (gated on the frontend side).
    ///
    /// Direction values: "up", "down", "left", "right",
    ///   or combos: "left-right", "down-up", "down-left", "down-right", "left-up", "right-up"
    /// </summary>
    public async Task CarDirectionChange(CarDirectionRequest request)
    {
        _logger.LogInformation(
            "🎮 Car direction change from {ConnectionId}: {Direction}",
            Context.ConnectionId, request.Direction);

        var payload = new
        {
            ConnectionId = Context.ConnectionId,
            Direction = request.Direction,
            Speed = request.Speed
        };
        await ForwardToJessicaManagerAsync("/api/car/direction", payload, Context.ConnectionAborted).ConfigureAwait(false);
    }

    /// <summary>
    /// Receives a car speed change from the frontend speed dial.
    /// Speed is an integer between 0 (min) and 100 (max).
    /// </summary>
    public async Task CarSpeedChange(CarSpeedRequest request)
    {
        _logger.LogInformation(
            "🏎 Car speed change from {ConnectionId}: {Speed}",
            Context.ConnectionId, request.Speed);

        var payload = new
        {
            ConnectionId = Context.ConnectionId,
            Speed = request.Speed
        };
        await ForwardToJessicaManagerAsync("/api/car/speed", payload, Context.ConnectionAborted).ConfigureAwait(false);
    }

    /// <summary>
    /// Client signals that the car has started (user pressed Start).
    /// </summary>
    public async Task CarStart()
    {
        _logger.LogInformation("▶ Car START from {ConnectionId}", Context.ConnectionId);

        var payload = new
        {
            ConnectionId = Context.ConnectionId
        };
        await ForwardToJessicaManagerAsync("/api/car/start", payload, Context.ConnectionAborted).ConfigureAwait(false);
    }

    /// <summary>
    /// Client signals that the car has stopped (user pressed Stop).
    /// </summary>
    public async Task CarStop()
    {
        _logger.LogInformation("⏹ Car STOP from {ConnectionId}", Context.ConnectionId);

        var payload = new
        {
            ConnectionId = Context.ConnectionId
        };
        await ForwardToJessicaManagerAsync("/api/car/stop", payload, Context.ConnectionAborted).ConfigureAwait(false);
    }

    private async Task ForwardToJessicaManagerAsync<TPayload>(string route, TPayload payload, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient("JessicaManager");

        var userId = Context.UserIdentifier;
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning(
                "JessicaManager forward skipped user id: {Route} from {ConnectionId}. JWT sub/NameIdentifier missing — car commands will be rejected.",
                route,
                Context.ConnectionId);
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, route)
        {
            Content = JsonContent.Create(payload)
        };

        if (!string.IsNullOrEmpty(userId))
        {
            request.Headers.TryAddWithoutValidation("X-User-Id", userId);
        }

        try
        {
            var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "JessicaManager rejected command {Route} from {ConnectionId}. StatusCode={StatusCode}",
                    route,
                    Context.ConnectionId,
                    (int)response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed forwarding command {Route} from {ConnectionId} to JessicaManager",
                route,
                Context.ConnectionId);
        }
    }
}
