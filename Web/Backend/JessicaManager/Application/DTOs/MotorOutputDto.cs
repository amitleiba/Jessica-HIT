namespace JessicaManager.Application.DTOs;

public sealed class MotorOutputDto
{
    public int LeftMotor { get; init; }
    public int RightMotor { get; init; }
    public string NormalizedDirection { get; init; } = "Idle";
}
