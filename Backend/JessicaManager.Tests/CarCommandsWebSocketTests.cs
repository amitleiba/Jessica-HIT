using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Globalization;
using JessicaManager.Application.Adapters;
using JessicaManager.Infrastructure.Options;
using JessicaManager.Infrastructure.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace JessicaManager.Tests;

public class CarCommandsWebSocketTests
{
    [Theory]
    [InlineData("up", 50, 50)]
    [InlineData("down", -50, -50)]
    [InlineData("left", -32, 32)]
    [InlineData("right", 32, -32)]
    [InlineData("up-left", 22, 50)]
    [InlineData("left-up", 22, 50)]
    [InlineData("up-right", 50, 22)]
    [InlineData("right-up", 50, 22)]
    [InlineData("down-left", -22, -50)]
    [InlineData("left-down", -22, -50)]
    [InlineData("down-right", -50, -22)]
    [InlineData("right-down", -50, -22)]
    [InlineData("idle", 0, 0)]
    [InlineData("left-right", 0, 0)]
    [InlineData("down-up", 0, 0)]
    [InlineData("unknown-direction", 0, 0)]
    public async Task DirectionEndpoint_ConvertsAndPublishesWheelValues_ForAllDirections(
        string direction,
        int expectedLeftWheel,
        int expectedRightWheel)
    {
        var publisherSpy = new MoveCommandPublisherSpy();

        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton<IMoveCommandPublisher>(publisherSpy);
                });
            });

        using var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/car/direction", new
        {
            connectionId = "test-connection",
            direction,
            speed = 50
        });

        response.EnsureSuccessStatusCode();

        var published = await publisherSpy.WaitForPublishAsync();
        Assert.Equal(expectedLeftWheel, published.LeftWheel);
        Assert.Equal(expectedRightWheel, published.RightWheel);
    }

    [Fact]
    public async Task WebSocketPublisher_SendsExpectedMoveJsonPayload()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var receivedPayloadTcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var port = GetFreeTcpPort();
        var httpUrl = $"http://127.0.0.1:{port}";
        var wsUrl = $"ws://127.0.0.1:{port}/ws";

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseSetting("urls", httpUrl);
        var app = builder.Build();
        app.UseWebSockets();
        app.Map("/ws", async context =>
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return;
            }

            using var socket = await context.WebSockets.AcceptWebSocketAsync().ConfigureAwait(false);
            var buffer = new byte[512];
            var result = await socket.ReceiveAsync(buffer, cts.Token).ConfigureAwait(false);
            var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
            receivedPayloadTcs.TrySetResult(message);
        });

        await app.StartAsync(cts.Token);
        try
        {
            var publisher = new JessicaWebSocketMoveCommandPublisher(
                NullLogger<JessicaWebSocketMoveCommandPublisher>.Instance,
                Options.Create(new JessicaWebSocketOptions { Url = new Uri(wsUrl) }),
                new InMemoryRobotStatusState());

            await publisher.PublishMoveCommandAsync(12, -34, cts.Token);
            var payload = await receivedPayloadTcs.Task.WaitAsync(TimeSpan.FromSeconds(5), cts.Token);

            Assert.Equal("""{"leftWheel":12,"rightWheel":-34}""", payload);
        }
        finally
        {
            await app.StopAsync(cts.Token);
            await app.DisposeAsync();
        }
    }

    [Theory]
    [InlineData(49, 0, 0, 3.3)]
    [InlineData(120, 1, 1, 4.15)]
    [InlineData(0, 0, 1, 3.01)]
    [InlineData(999, 1, 0, 3.999)]
    public async Task WebSocketPublisher_ReceivesAndParsesStatusEventsCorrectly(
        int expectedDistance,
        int expectedSafety,
        int expectedMode,
        double expectedBattery)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var port = GetFreeTcpPort();
        var httpUrl = $"http://127.0.0.1:{port}";
        var wsUrl = $"ws://127.0.0.1:{port}/ws";
        var payload = FormattableString.Invariant(
            $@"{{""distance"":{expectedDistance},""safety"":{expectedSafety},""mode"":{expectedMode},""battery"":{expectedBattery}}}");

        var messageSentTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var statusState = new InMemoryRobotStatusState();

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseSetting("urls", httpUrl);
        var app = builder.Build();
        app.UseWebSockets();
        app.Map("/ws", async context =>
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return;
            }

            using var socket = await context.WebSockets.AcceptWebSocketAsync().ConfigureAwait(false);
            await socket.SendAsync(Encoding.UTF8.GetBytes(payload), WebSocketMessageType.Text, true, cts.Token).ConfigureAwait(false);
            messageSentTcs.TrySetResult();
            await Task.Delay(200, cts.Token).ConfigureAwait(false);
        });

        await app.StartAsync(cts.Token);
        var publisher = new JessicaWebSocketMoveCommandPublisher(
            NullLogger<JessicaWebSocketMoveCommandPublisher>.Instance,
            Options.Create(new JessicaWebSocketOptions { Url = new Uri(wsUrl) }),
            statusState);

        try
        {
            await publisher.StartAsync(cts.Token);
            await messageSentTcs.Task.WaitAsync(TimeSpan.FromSeconds(5), cts.Token);
            var status = await WaitForStatusAsync(statusState, cts.Token);

            Assert.Equal(expectedDistance, status.Distance);
            Assert.Equal(expectedSafety, status.Safety);
            Assert.Equal(expectedMode, status.Mode);
            Assert.Equal(expectedBattery, status.Battery, 4);
        }
        finally
        {
            await publisher.StopAsync(CancellationToken.None);
            publisher.Dispose();
            await app.StopAsync(cts.Token);
            await app.DisposeAsync();
        }
    }

    private static int GetFreeTcpPort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    private static async Task<JessicaManager.Application.DTOs.RobotStatusDto> WaitForStatusAsync(
        InMemoryRobotStatusState statusState,
        CancellationToken cancellationToken)
    {
        var timeoutAt = DateTime.UtcNow.AddSeconds(5);
        while (DateTime.UtcNow < timeoutAt)
        {
            if (statusState.TryGetLatest(out var status) && status is not null)
            {
                return status;
            }

            await Task.Delay(50, cancellationToken).ConfigureAwait(false);
        }

        throw new TimeoutException("Timed out waiting for robot status event.");
    }

    private sealed class MoveCommandPublisherSpy : IMoveCommandPublisher
    {
        private readonly TaskCompletionSource<(int LeftWheel, int RightWheel)> _publishedTcs =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task PublishMoveCommandAsync(int leftWheel, int rightWheel, CancellationToken cancellationToken)
        {
            _publishedTcs.TrySetResult((leftWheel, rightWheel));
            return Task.CompletedTask;
        }

        public async Task<(int LeftWheel, int RightWheel)> WaitForPublishAsync()
        {
            return await _publishedTcs.Task.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        }
    }
}
