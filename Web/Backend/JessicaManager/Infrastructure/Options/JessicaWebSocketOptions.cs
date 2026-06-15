namespace JessicaManager.Infrastructure.Options;

public sealed class JessicaWebSocketOptions
{
    public const string SectionName = "JessicaWebSocket";

    public Uri Url { get; set; } = new("ws://localhost:8080/ws");
}
