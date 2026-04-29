namespace Gateway.API.DTOs.Responses;

public sealed class RobotStatusResponse
{
    public bool Available { get; init; }

    public int Distance { get; init; }

    public int Safety { get; init; }

    public int Mode { get; init; }

    public double Battery { get; init; }

    public DateTime ReceivedAtUtc { get; init; }
}
