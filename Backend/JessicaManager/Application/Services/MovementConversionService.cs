using JessicaManager.Application.Adapters;
using JessicaManager.Application.DTOs;

namespace JessicaManager.Application.Services;

public sealed class MovementConversionService : IMovementConversionService
{
    // Future change point: when UI speed becomes 0..10, change this constant to 10.
    private const int MaxSpeedInput = 100;

    private static readonly Dictionary<MovementDirection, (int left, int right)> BaseOutputs =
        new()
        {
            [MovementDirection.Idle] = (0, 0),
            [MovementDirection.Forward] = (100, 100),
            [MovementDirection.Backward] = (-100, -100),
            [MovementDirection.RotateLeft] = (-65, 65),
            [MovementDirection.RotateRight] = (65, -65),
            [MovementDirection.ForwardLeft] = (45, 100),
            [MovementDirection.ForwardRight] = (100, 45),
            [MovementDirection.BackwardLeft] = (-45, -100),
            [MovementDirection.BackwardRight] = (-100, -45)
        };

    public MotorOutputDto ConvertToMotorOutput(MovementConversionRequestDto request)
    {
        var direction = ParseDirection(request.Direction);
        var clampedSpeed = Math.Clamp(request.Speed, 0, MaxSpeedInput);
        var speedFactor = (double)clampedSpeed / MaxSpeedInput;

        var (baseLeft, baseRight) = BaseOutputs[direction];
        var leftMotor = Math.Clamp((int)Math.Round(baseLeft * speedFactor), -100, 100);
        var rightMotor = Math.Clamp((int)Math.Round(baseRight * speedFactor), -100, 100);

        return new MotorOutputDto
        {
            LeftMotor = leftMotor,
            RightMotor = rightMotor,
            NormalizedDirection = direction.ToString()
        };
    }

    private static MovementDirection ParseDirection(string rawDirection)
    {
        if (string.IsNullOrWhiteSpace(rawDirection))
        {
            return MovementDirection.Idle;
        }

        return rawDirection.Trim().ToUpperInvariant() switch
        {
            "UP" => MovementDirection.Forward,
            "DOWN" => MovementDirection.Backward,
            "LEFT" => MovementDirection.RotateLeft,
            "RIGHT" => MovementDirection.RotateRight,
            "LEFT-UP" => MovementDirection.ForwardLeft,
            "UP-LEFT" => MovementDirection.ForwardLeft,
            "RIGHT-UP" => MovementDirection.ForwardRight,
            "UP-RIGHT" => MovementDirection.ForwardRight,
            "DOWN-LEFT" => MovementDirection.BackwardLeft,
            "LEFT-DOWN" => MovementDirection.BackwardLeft,
            "DOWN-RIGHT" => MovementDirection.BackwardRight,
            "RIGHT-DOWN" => MovementDirection.BackwardRight,
            "IDLE" => MovementDirection.Idle,
            "LEFT-RIGHT" => MovementDirection.Idle,
            "DOWN-UP" => MovementDirection.Idle,
            _ => MovementDirection.Idle
        };
    }

    private enum MovementDirection
    {
        Idle,
        Forward,
        Backward,
        RotateLeft,
        RotateRight,
        ForwardLeft,
        ForwardRight,
        BackwardLeft,
        BackwardRight
    }
}
