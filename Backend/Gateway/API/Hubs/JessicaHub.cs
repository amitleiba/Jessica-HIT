using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Gateway.API.DTOs.Requests;

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
public class JessicaHub(ILogger<JessicaHub> logger) : Hub
{
    private readonly ILogger<JessicaHub> _logger = logger;

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

        // TODO: Forward the direction to the Jessica Manager microservice
        // e.g. await _jessicaManagerClient.SendDirectionAsync(request.Direction);

        await Task.CompletedTask;
    }

    /// <summary>
    /// Client signals that the car has started (user pressed Start).
    /// </summary>
    public async Task CarStart()
    {
        _logger.LogInformation("▶ Car START from {ConnectionId}", Context.ConnectionId);

        // TODO: Notify Jessica Manager that this user's car session started
        // e.g. await _jessicaManagerClient.StartSessionAsync(Context.ConnectionId);

        await Task.CompletedTask;
    }

    /// <summary>
    /// Client signals that the car has stopped (user pressed Stop).
    /// </summary>
    public async Task CarStop()
    {
        _logger.LogInformation("⏹ Car STOP from {ConnectionId}", Context.ConnectionId);

        // TODO: Notify Jessica Manager that this user's car session stopped
        // e.g. await _jessicaManagerClient.StopSessionAsync(Context.ConnectionId);

        await Task.CompletedTask;
    }
}
