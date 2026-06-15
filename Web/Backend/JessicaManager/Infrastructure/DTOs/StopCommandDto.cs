using System.Text.Json.Serialization;

namespace JessicaManager.Infrastructure.DTOs;

public sealed class StopCommandDto
{
    [JsonPropertyName("cmd")]
    public string Cmd { get; init; } = "stop";
}
