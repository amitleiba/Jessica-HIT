using System.Text.Json.Serialization;

namespace JessicaManager.Infrastructure.DTOs;

public sealed class RobotStatusEventDto
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;

    [JsonPropertyName("distance")]
    public int Distance { get; init; }

    [JsonPropertyName("safety")]
    public int Safety { get; init; }

    [JsonPropertyName("mode")]
    public int Mode { get; init; }

    [JsonPropertyName("battery")]
    public double Battery { get; init; }
}
