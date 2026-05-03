using System.Text.Json.Serialization;

namespace JessicaManager.Infrastructure.DTOs;

public sealed class MoveCommandDto
{
    [JsonPropertyName("cmd")]
    public string Cmd { get; init; } = "move";

    [JsonPropertyName("left")]
    public int Left { get; init; }

    [JsonPropertyName("right")]
    public int Right { get; init; }
}
