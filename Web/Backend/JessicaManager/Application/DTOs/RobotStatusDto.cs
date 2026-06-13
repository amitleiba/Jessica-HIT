namespace JessicaManager.Application.DTOs;

public sealed class RobotStatusDto
{
    public int Distance { get; init; }

    public int Safety { get; init; }

    public int Mode { get; init; }

    public double Battery { get; init; }

    public DateTime ReceivedAtUtc { get; init; } = DateTime.UtcNow;
}
