using System.Text.Json;

namespace JessicaManager.Infrastructure.Services;

/// <summary>
/// Write-only service that persists the ESP32 Gateway URL to <c>gateway-ip.json</c>
/// in the application content root.  Reading is handled natively by the .NET
/// configuration pipeline via <c>AddJsonFile("gateway-ip.json", reloadOnChange: true)</c>,
/// which makes <see cref="Microsoft.Extensions.Options.IOptionsMonitor{T}"/> fire
/// automatically whenever the file changes on disk.
/// </summary>
public sealed class GatewayIpPersistenceService(IWebHostEnvironment env, ILogger<GatewayIpPersistenceService> logger)
{
    private readonly string _filePath = Path.Combine(env.ContentRootPath, "gateway-ip.json");

    private static readonly JsonSerializerOptions _writeOptions = new() { WriteIndented = true };

    /// <summary>
    /// Converts the caller-supplied HTTP/HTTPS base URL to its WebSocket equivalent
    /// and writes it to <c>gateway-ip.json</c>. The configuration system picks up
    /// the change automatically via <see cref="Microsoft.Extensions.Options.IOptionsMonitor{T}"/>.
    /// </summary>
    /// <param name="url">
    ///   Full base URL of the ESP32 Gateway, e.g. <c>http://192.168.1.215:81</c>.
    ///   Accepted schemes: <c>http</c>, <c>https</c>, <c>ws</c>, <c>wss</c>.
    /// </param>
    public void SaveGatewayUrl(string url)
    {
        var wsUrl = ConvertToWebSocketUrl(url);

        var payload = new Dictionary<string, object>
        {
            ["JessicaWebSocket"] = new Dictionary<string, string>
            {
                ["Url"] = wsUrl
            }
        };

        var json = JsonSerializer.Serialize(payload, _writeOptions);
        File.WriteAllText(_filePath, json);

        logger.LogInformation(
            "📝 Gateway URL persisted. InputUrl={InputUrl}, WsUrl={WsUrl}, File={File}",
            url, wsUrl, _filePath);
    }

    /// <summary>
    /// Converts an HTTP/HTTPS base URL to its WebSocket (ws/wss) equivalent
    /// and appends the <c>/ws</c> path expected by the ESP32 Gateway firmware.
    /// </summary>
    private static string ConvertToWebSocketUrl(string url)
    {
        var trimmed = url.Trim().TrimEnd('/');

        // Already a WebSocket URL — return as-is.
        if (trimmed.StartsWith("ws://", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("wss://", StringComparison.OrdinalIgnoreCase))
        {
            return trimmed;
        }

        // Convert http → ws, https → wss.
        if (trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return "wss://" + trimmed["https://".Length..];

        if (trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            return "ws://" + trimmed["http://".Length..];

        // Bare host — assume ws.
        return "ws://" + trimmed;
    }
}
