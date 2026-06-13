using JessicaManager.Application.Adapters;
using JessicaManager.Application.DTOs;

namespace JessicaManager.Infrastructure.Services;

public sealed class InMemoryRobotStatusState : IRobotStatusState
{
    private readonly object _sync = new();
    private RobotStatusDto? _latest;

    public void Update(RobotStatusDto status)
    {
        lock (_sync)
        {
            _latest = status;
        }
    }

    public bool TryGetLatest(out RobotStatusDto? status)
    {
        lock (_sync)
        {
            status = _latest;
            return status is not null;
        }
    }
}
