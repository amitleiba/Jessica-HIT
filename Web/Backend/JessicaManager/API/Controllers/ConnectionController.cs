using Microsoft.AspNetCore.Mvc;
using JessicaManager.Application.Adapters;
using JessicaManager.Infrastructure.Options;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;
using System.Net.WebSockets;

namespace JessicaManager.API.Controllers;

[ApiController]
[Route("api/connection")]
public class ConnectionController(
    ILogger<ConnectionController> logger,
    IConnectionManager connectionManager,
    IOptionsMonitor<JessicaWebSocketOptions> wsOptions) : ControllerBase
{
    private readonly ILogger<ConnectionController> _logger = logger;
    private readonly IConnectionManager _connectionManager = connectionManager;
    private readonly IOptionsMonitor<JessicaWebSocketOptions> _wsOptions = wsOptions;

    private string? GetUserId() => Request.Headers["X-User-Id"].FirstOrDefault();

    /// <summary>
    /// Updates the ESP32 Gateway IP. Persists to gateway-ip.json and reconnects.
    /// </summary>
    [HttpPut("gateway-ip")]
    public async Task<IActionResult> UpdateGatewayIp(
        [FromBody] UpdateGatewayIpRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(GetUserId()))
            return Unauthorized(new { message = "User identity not provided" });

        if (!IsValidGatewayUrl(request.Url))
        {
            return BadRequest(new { message = $"'{request.Url}' is not a valid http/https URL." });
        }

        _logger.LogInformation(
            "🌐 Gateway URL update requested. NewUrl={Url}, UserId={UserId}",
            request.Url, GetUserId());

        await _connectionManager.UpdateGatewayUrlAsync(request.Url, cancellationToken).ConfigureAwait(false);

        return Ok(new { success = true, url = request.Url });
    }

    /// <summary>
    /// Diagnostic: returns the currently configured WebSocket URL and attempts
    /// a test connection so you can see the exact error the ESP32 is returning.
    /// </summary>
    [HttpGet("status")]
    public async Task<IActionResult> Status(CancellationToken cancellationToken)
    {
        var targetUrl = _wsOptions.CurrentValue.Url;
        string? error = null;
        bool reachable = false;

        try
        {
            using var testSocket = new ClientWebSocket();
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(4));

            await testSocket.ConnectAsync(targetUrl, cts.Token).ConfigureAwait(false);
            reachable = true;
            await testSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "diag", CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            error = ex.Message;
        }

        return Ok(new
        {
            targetUrl = targetUrl.ToString(),
            reachable,
            error
        });
    }

    /// <summary>
    /// Forces an immediate reconnect to the ESP32 Gateway and robot.
    /// </summary>
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(GetUserId()))
            return Unauthorized(new { message = "User identity not provided" });

        _logger.LogInformation(
            "🔄 Connection refresh requested. UserId={UserId}", GetUserId());

        await _connectionManager.ForceReconnectAsync(cancellationToken).ConfigureAwait(false);

        return Ok(new { success = true });
    }

    private static bool IsValidGatewayUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;
        return Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri)
               && (uri.Scheme == Uri.UriSchemeHttp
                   || uri.Scheme == Uri.UriSchemeHttps
                   || uri.Scheme == "ws"
                   || uri.Scheme == "wss");
    }
}

public sealed class UpdateGatewayIpRequest
{
    [Required]
    public string Url { get; init; } = string.Empty;
}
