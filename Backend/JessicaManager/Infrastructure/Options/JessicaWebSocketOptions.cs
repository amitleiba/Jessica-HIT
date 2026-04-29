namespace JessicaManager.Infrastructure.Options;

public sealed class JessicaWebSocketOptions
{
    public const string SectionName = "JessicaWebSocket";

    public Uri Url { get; init; } = new("ws://localhost:8080/ws");
}
