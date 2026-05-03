using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using JessicaManager.Application.Adapters;
using JessicaManager.Application.DTOs;
using JessicaManager.Infrastructure.DTOs;
using JessicaManager.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace JessicaManager.Infrastructure.Services;

public sealed class JessicaWebSocketMoveCommandPublisher(
    ILogger<JessicaWebSocketMoveCommandPublisher> logger,
    IOptions<JessicaWebSocketOptions> options,
    IRobotStatusState robotStatusState) : IMoveCommandPublisher, IHostedService, IDisposable
{
    private readonly ILogger<JessicaWebSocketMoveCommandPublisher> _logger = logger;
    private readonly JessicaWebSocketOptions _options = options.Value;
    private readonly IRobotStatusState _robotStatusState = robotStatusState;
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);
    private ClientWebSocket? _socket;
    private CancellationTokenSource? _lifecycleCts;
    private Task? _receiveSupervisorTask;
    private bool _disposed;
    private int _receiveFailureCount;
    private DateTime _lastDetailedFailureLogUtc = DateTime.MinValue;

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

            await _socket!.SendAsync(
                new ArraySegment<byte>(bytes),
                WebSocketMessageType.Text,
                endOfMessage: true,
                cancellationToken).ConfigureAwait(false);

            _logger.LogInformation(
                "📤 Move command sent to Jessica over WS. LeftWheel={LeftWheel}, RightWheel={RightWheel}, Url={Url}",
                leftWheel,
                rightWheel,
                _options.Url);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "❌ Failed sending move command over WS. LeftWheel={LeftWheel}, RightWheel={RightWheel}, Url={Url}",
                leftWheel,
                rightWheel,
                _options.Url);

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

            await _socket!.SendAsync(
                new ArraySegment<byte>(bytes),
                WebSocketMessageType.Text,
                endOfMessage: true,
                cancellationToken).ConfigureAwait(false);

            _logger.LogInformation(
                "📤 Stop command sent to Jessica over WS. Url={Url}",
                _options.Url);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "❌ Failed sending stop command over WS. Url={Url}",
                _options.Url);

            await ResetSocketAsync().ConfigureAwait(false);
            throw;
        }
        finally
        {
            _sendLock.Release();
        }
    }

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
            _logger.LogInformation("🔌 Connecting to Jessica WS endpoint: {Url}", _options.Url);
            await _socket.ConnectAsync(_options.Url, cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("✅ Connected to Jessica WS endpoint: {Url}", _options.Url);
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

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _lifecycleCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
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
                    _options.Url,
                    delay.TotalSeconds,
                    _receiveFailureCount);
            }
            else
            {
                _logger.LogDebug(
                    "Jessica WS still returning 404. Url={Url}. RetryIn={RetryIn}s, FailureCount={FailureCount}",
                    _options.Url,
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
