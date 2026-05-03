using Microsoft.AspNetCore.Mvc;
using JessicaManager.Application.Adapters;
using JessicaManager.Application.DTOs;

namespace JessicaManager.API.Controllers;

[ApiController]
[Route("api/car")]
public class CarCommandsController(
    ILogger<CarCommandsController> logger,
    IMovementConversionService movementConversionService,
    IMoveCommandPublisher moveCommandPublisher,
    IRobotStatusState robotStatusState) : ControllerBase
{
    private readonly ILogger<CarCommandsController> _logger = logger;
    private readonly IMovementConversionService _movementConversionService = movementConversionService;
    private readonly IMoveCommandPublisher _moveCommandPublisher = moveCommandPublisher;
    private readonly IRobotStatusState _robotStatusState = robotStatusState;

    [HttpPost("direction")]
    public async Task<IActionResult> Direction([FromBody] CarDirectionCommand command, CancellationToken cancellationToken)
    {
        var output = _movementConversionService.ConvertToMotorOutput(
            new MovementConversionRequestDto
            {
                Direction = command.Direction,
                Speed = command.Speed
            });

        _logger.LogInformation(
            "🎮 Direction command received. ConnectionId={ConnectionId}, Direction={Direction}, Speed={Speed}, LeftMotor={LeftMotor}, RightMotor={RightMotor}, NormalizedDirection={NormalizedDirection}",
            command.ConnectionId,
            command.Direction,
            command.Speed,
            output.LeftMotor,
            output.RightMotor,
            output.NormalizedDirection);

        await _moveCommandPublisher
            .PublishMoveCommandAsync(output.LeftMotor, output.RightMotor, cancellationToken)
            .ConfigureAwait(false);

        return Ok(new
        {
            accepted = true,
            direction = output.NormalizedDirection,
            speed = command.Speed,
            leftMotor = output.LeftMotor,
            rightMotor = output.RightMotor
        });
    }

    [HttpPost("speed")]
    public IActionResult Speed([FromBody] CarSpeedCommand command)
    {
        _logger.LogInformation(
            "🏎 Speed command received. ConnectionId={ConnectionId}, Speed={Speed}",
            command.ConnectionId,
            command.Speed);
        return Ok(new { accepted = true, speed = command.Speed });
    }

    [HttpPost("start")]
    public IActionResult Start([FromBody] CarSessionCommand command)
    {
        _logger.LogInformation(
            "▶ Start command received. ConnectionId={ConnectionId}",
            command.ConnectionId);
        return Ok(new { accepted = true });
    }

    [HttpPost("stop")]
    public async Task<IActionResult> Stop([FromBody] CarSessionCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "⏹ Stop command received. ConnectionId={ConnectionId}",
            command.ConnectionId);

        await _moveCommandPublisher
            .PublishStopCommandAsync(cancellationToken)
            .ConfigureAwait(false);

        return Ok(new
        {
            accepted = true,
            leftMotor = 0,
            rightMotor = 0
        });
    }

    [HttpGet("status")]
    public IActionResult Status()
    {
        if (!_robotStatusState.TryGetLatest(out var status) || status is null)
        {
            return NotFound(new
            {
                available = false,
                message = "No robot status has been received yet."
            });
        }

        return Ok(new
        {
            available = true,
            distance = status.Distance,
            safety = status.Safety,
            mode = status.Mode,
            battery = status.Battery,
            receivedAtUtc = status.ReceivedAtUtc
        });
    }
}

public sealed class CarDirectionCommand
{
    public string ConnectionId { get; init; } = string.Empty;

    public string Direction { get; init; } = string.Empty;

    public int Speed { get; init; }
}

public sealed class CarSpeedCommand
{
    public string ConnectionId { get; init; } = string.Empty;

    public int Speed { get; init; }
}

public sealed class CarSessionCommand
{
    public string ConnectionId { get; init; } = string.Empty;
}
