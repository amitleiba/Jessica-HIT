using JessicaManager.Application.DTOs;

namespace JessicaManager.Application.Adapters;

public interface IRobotStatusState
{
    void Update(RobotStatusDto status);

    bool TryGetLatest(out RobotStatusDto? status);
}
