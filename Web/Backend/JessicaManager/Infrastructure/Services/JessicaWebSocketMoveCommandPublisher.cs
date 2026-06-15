using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using JessicaManager.Application.Adapters;
using JessicaManager.Application.DTOs;
using JessicaManager.Infrastructure.DTOs;
using JessicaManager.Infrastructure.Options;
using JessicaManager.Infrastructure.Services;
using Microsoft.Extensions.Options;

namespace JessicaManager.Infrastructure.Services;

public sealed class JessicaWebSocketMoveCommandPublisher(
    ILogger<JessicaWebSocketMoveCommandPublisher> logger,
    IOptionsMonitor<JessicaWebSocketOptions> optionsMonitor,
    IRobotStatusState robotStatusState,
    GatewayIpPersistenceService gatewayIpPersistence) : IMoveCommandPublisher, IConnectionManager, IHostedService, IDisposable
{
    private readonly ILogger<JessicaWebSocketMoveCommandPublisher> _logger = logger;
    private readonly IRobotStatusState _robotStatusState = robotStatusState;
    private readonly GatewayIpPersistenceService _gatewayIpPersistence = gatewayIpPersistence;
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);
    private ClientWebSocket? _socket;
    private CancellationTokenSource? _lifecycleCts;
    private Task? _receiveSupervisorTask;
    private bool _disposed;
    private int _receiveFailureCount;
    private DateTime _lastDetailedFailureLogUtc = DateTime.MinValue;

    // Always read the current URL from the live monitor snapshot.
    private Uri CurrentUrl => optionsMonitor.CurrentValue.Url;

    public async Task PublishMoveCommandAsync(int leftWheel, int rightWheel, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        await _sendLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);

            var payload = new MoveCommandDto
            {
                Cmd = "move",
                Left = leftWheel,
                Right = rightWheel
            };
            var json = JsonSerializer.Serialize(payload, _jsonOptions);
            var bytes = Encoding.UTF8.GetBytes(json);

            try
            {
                await _socket!.SendAsync(
                    new ArraySegment<byte>(bytes),
                    WebSocketMessageType.Text,
                    endOfMessage: true,
                    cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception firstEx)
            {
                _logger.LogWarning(
                    firstEx,
                    "⚠ WS send failed on first attempt; reconnecting and retrying once. LeftWheel={LeftWheel}, RightWheel={RightWheel}",
                    leftWheel,
                    rightWheel);

                await ResetSocketAsync().ConfigureAwait(false);
                await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);

                await _socket!.SendAsync(
                    new ArraySegment<byte>(bytes),
                    WebSocketMessageType.Text,
                    endOfMessage: true,
                    cancellationToken).ConfigureAwait(false);
            }

            _logger.LogInformation(
                "📤 Move command sent to Jessica over WS. LeftWheel={LeftWheel}, RightWheel={RightWheel}, Url={Url}",
                leftWheel,
                rightWheel,
                CurrentUrl);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "❌ Failed sending move command over WS. LeftWheel={LeftWheel}, RightWheel={RightWheel}, Url={Url}",
                leftWheel,
                rightWheel,
                CurrentUrl);

            await ResetSocketAsync().ConfigureAwait(false);
            throw;
        }
        finally
        {
            _sendLock.Release();
        }
    }

    public async Task PublishStopCommandAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        await _sendLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);

            var payload = new StopCommandDto();
            var json = JsonSerializer.Serialize(payload, _jsonOptions);
            var bytes = Encoding.UTF8.GetBytes(json);

            try
            {
                await _socket!.SendAsync(
                    new ArraySegment<byte>(bytes),
                    WebSocketMessageType.Text,
                    endOfMessage: true,
                    cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception firstEx)
            {
                _logger.LogWarning(
                    firstEx,
                    "⚠ WS send failed on first attempt; reconnecting and retrying once.");

                await ResetSocketAsync().ConfigureAwait(false);
                await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);

                await _socket!.SendAsync(
                    new ArraySegment<byte>(bytes),
                    WebSocketMessageType.Text,
                    endOfMessage: true,
                    cancellationToken).ConfigureAwait(false);
            }

            _logger.LogInformation(
                "📤 Stop command sent to Jessica over WS. Url={Url}",
                CurrentUrl);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "❌ Failed sending stop command over WS. Url={Url}",
                CurrentUrl);

            await ResetSocketAsync().ConfigureAwait(false);
            throw;
        }
        finally
        {
            _sendLock.Release();
        }
    }

    // ─────────────────────────────────────────────
    //  IConnectionManager
    // ─────────────────────────────────────────────

    /// <inheritdoc />
    public async Task ForceReconnectAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔄 Force-reconnect requested. Resetting socket…");
        await ResetSocketAsync().ConfigureAwait(false);
        _receiveFailureCount = 0;
        // The receive supervisor loop will pick up the reconnect automatically.
    }

    /// <inheritdoc />
    public async Task UpdateGatewayUrlAsync(string url, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🌐 Updating Gateway URL to {Url}…", url);

        // Persist to disk — IOptionsMonitor fires OnChange automatically.
        _gatewayIpPersistence.SaveGatewayUrl(url);

        // Give the config system a brief moment to reload the file,
        // then force an immediate reconnect so we don't wait for the next retry cycle.
        await Task.Delay(200, cancellationToken).ConfigureAwait(false);
        await ForceReconnectAsync(cancellationToken).ConfigureAwait(false);
    }

    // ─────────────────────────────────────────────
    //  Connection helpers
    // ─────────────────────────────────────────────

    private async Task EnsureConnectedAsync(CancellationToken cancellationToken)
    {
        await _connectionLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_socket?.State == WebSocketState.Open)
            {
                return;
            }

            await ResetSocketAsync().ConfigureAwait(false);

            _socket = new ClientWebSocket();
            _logger.LogInformation("🔌 Connecting to Jessica WS endpoint: {Url}", CurrentUrl);
            await _socket.ConnectAsync(CurrentUrl, cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("✅ Connected to Jessica WS endpoint: {Url}", CurrentUrl);
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    private async Task ResetSocketAsync()
    {
        if (_socket is null)
        {
            return;
        }

        try
        {
            if (_socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
            {
                await _socket.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "Resetting Jessica manager socket",
                    CancellationToken.None).ConfigureAwait(false);
            }
        }
        catch
        {
            // Best-effort shutdown only.
        }
        finally
        {
            _socket.Dispose();
            _socket = null;
        }
    }

    // ─────────────────────────────────────────────
    //  IHostedService
    // ─────────────────────────────────────────────

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _lifecycleCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        // NOTE: We deliberately do NOT subscribe to optionsMonitor.OnChange here.
        // The .NET FileSystemWatcher fires two change events per file write, which
        // causes a race condition during reconnect. Instead, the new URL is read
        // lazily via CurrentUrl => optionsMonitor.CurrentValue.Url on every
        // EnsureConnectedAsync call — so UpdateGatewayUrlAsync's explicit
        // ForceReconnectAsync is all that's needed to pick up a changed address.

        _receiveSupervisorTask = Task.Run(() => RunReceiveSupervisorAsync(_lifecycleCts.Token), CancellationToken.None);
        _logger.LogInformation("🚀 Jessica WS receive supervisor started.");
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_lifecycleCts is not null)
        {
            await _lifecycleCts.CancelAsync().ConfigureAwait(false);
        }

        if (_receiveSupervisorTask is not null)
        {
            try
            {
                await _receiveSupervisorTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Expected during graceful shutdown.
            }
        }

        await ResetSocketAsync().ConfigureAwait(false);
    }

    // ─────────────────────────────────────────────
    //  Supervisor / Receive loop
    // ─────────────────────────────────────────────

    private async Task RunReceiveSupervisorAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
                _receiveFailureCount = 0;
                await ReceiveLoopAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _receiveFailureCount++;
                var delay = GetRetryDelay(_receiveFailureCount);
                LogReceiveFailure(ex, delay);
                await ResetSocketAsync().ConfigureAwait(false);
                try
                {
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }
    }

    private static TimeSpan GetRetryDelay(int failureCount)
    {
        var seconds = Math.Min(30, Math.Pow(2, Math.Min(failureCount - 1, 5)));
        return TimeSpan.FromSeconds(seconds);
    }

    private void LogReceiveFailure(Exception ex, TimeSpan delay)
    {
        var isWs404 = ex is WebSocketException wsEx && wsEx.Message.Contains("404", StringComparison.OrdinalIgnoreCase);
        var nowUtc = DateTime.UtcNow;
        var shouldLogDetailed = _receiveFailureCount <= 2 ||
                                _receiveFailureCount % 10 == 0 ||
                                nowUtc - _lastDetailedFailureLogUtc > TimeSpan.FromMinutes(1);

        if (isWs404)
        {
            if (shouldLogDetailed)
            {
                _lastDetailedFailureLogUtc = nowUtc;
                _logger.LogWarning(
                    ex,
                    "⚠ Jessica WS endpoint not found (HTTP 404). Verify JessicaWebSocket:Url path. Url={Url}. RetryIn={RetryIn}s, FailureCount={FailureCount}",
                    CurrentUrl,
                    delay.TotalSeconds,
                    _receiveFailureCount);
            }
            else
            {
                _logger.LogDebug(
                    "Jessica WS still returning 404. Url={Url}. RetryIn={RetryIn}s, FailureCount={FailureCount}",
                    CurrentUrl,
                    delay.TotalSeconds,
                    _receiveFailureCount);
            }

            return;
        }

        if (shouldLogDetailed)
        {
            _lastDetailedFailureLogUtc = nowUtc;
            _logger.LogWarning(
                ex,
                "⚠ Jessica WS receive loop failed. Reconnecting. RetryIn={RetryIn}s, FailureCount={FailureCount}",
                delay.TotalSeconds,
                _receiveFailureCount);
        }
        else
        {
            _logger.LogDebug(
                "Jessica WS receive retry scheduled. RetryIn={RetryIn}s, FailureCount={FailureCount}",
                delay.TotalSeconds,
                _receiveFailureCount);
        }
    }

    private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
    {
        var buffer = new byte[1024];
        while (!cancellationToken.IsCancellationRequested && _socket?.State == WebSocketState.Open)
        {
            var messageBuilder = new StringBuilder();
            WebSocketReceiveResult? result;

            do
            {
                result = await _socket.ReceiveAsync(buffer, cancellationToken).ConfigureAwait(false);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    _logger.LogInformation("🔌 Jessica WS closed by remote endpoint.");
                    return;
                }

                messageBuilder.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
            } while (!result.EndOfMessage);

            var rawMessage = messageBuilder.ToString();
            HandleIncomingStatusMessage(rawMessage);
        }
    }

    private void HandleIncomingStatusMessage(string rawMessage)
    {
        try
        {
            var incoming = JsonSerializer.Deserialize<RobotStatusEventDto>(rawMessage, _jsonOptions);
            if (incoming is null)
            {
                return;
            }

            // Only process telemetry messages from the robot.
            if (!string.Equals(incoming.Type, "telemetry", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogDebug("Ignoring non-telemetry WS message. Type={Type}", incoming.Type);
                return;
            }

            var status = new RobotStatusDto
            {
                Distance = incoming.Distance,
                Safety = incoming.Safety,
                Mode = incoming.Mode,
                Battery = incoming.Battery,
                ReceivedAtUtc = DateTime.UtcNow
            };

            _robotStatusState.Update(status);

            _logger.LogInformation(
                "📡 Status event received. Distance={Distance}, Safety={Safety}, Mode={Mode}, Battery={Battery}",
                status.Distance,
                status.Safety,
                status.Mode,
                status.Battery);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠ Failed to parse Jessica status message: {RawMessage}", rawMessage);
        }
    }

    // ─────────────────────────────────────────────
    //  IDisposable
    // ─────────────────────────────────────────────

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _lifecycleCts?.Cancel();
        _lifecycleCts?.Dispose();
        _sendLock.Dispose();
        _connectionLock.Dispose();
        _socket?.Dispose();
    }
}
