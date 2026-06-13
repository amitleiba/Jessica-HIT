using JessicaManager.Application.DTOs;

namespace JessicaManager.Application.Adapters;

public interface IMovementConversionService
{
    MotorOutputDto ConvertToMotorOutput(MovementConversionRequestDto request);
}
