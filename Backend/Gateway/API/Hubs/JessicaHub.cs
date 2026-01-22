using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Gateway.API.Hubs;

/// <summary>
/// SignalR Hub for real-time communication with Jessica drones
/// Requires authentication via Keycloak JWT token
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
        _logger.LogInformation("User connected");
        await base.OnConnectedAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Called when a client disconnects from the hub
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("User disconnected");
        await base.OnDisconnectedAsync(exception).ConfigureAwait(false);
    }
}
