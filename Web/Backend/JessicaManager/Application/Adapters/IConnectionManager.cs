namespace JessicaManager.Application.Adapters;

/// <summary>
/// Manages the lifecycle of the hardware connections:
/// the ESP32 Gateway WebSocket and the robot itself.
/// </summary>
public interface IConnectionManager
{
    /// <summary>
    /// Forces an immediate reconnect to the ESP32 Gateway WebSocket.
    /// </summary>
    Task ForceReconnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the target Gateway URL, persists it to disk (which triggers
    /// <see cref="Microsoft.Extensions.Options.IOptionsMonitor{T}"/> automatically),
    /// and reconnects using the new address.
    /// </summary>
    /// <param name="url">Full base URL of the ESP32 Gateway, e.g. <c>http://192.168.1.215:81</c>.</param>
    Task UpdateGatewayUrlAsync(string url, CancellationToken cancellationToken = default);
}
