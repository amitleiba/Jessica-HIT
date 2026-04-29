using System.Text.Json.Serialization;

namespace JessicaManager.Infrastructure.DTOs;

public sealed class MoveCommandDto
{
    [JsonPropertyName("leftWheel")]
    public int LeftWheel { get; init; }

    [JsonPropertyName("rightWheel")]
    public int RightWheel { get; init; }
}
