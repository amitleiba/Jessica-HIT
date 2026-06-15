using JessicaManager.Application.Adapters;

namespace JessicaManager.Infrastructure.Services;

/// <summary>
/// In-memory thread-safe implementation of ICurrentSpeedState.
/// </summary>
public class InMemoryCurrentSpeedState : ICurrentSpeedState
{
    private int _speed = 50; // Default speed
    private readonly object _lock = new();

    public int GetSpeed()
    {
        lock (_lock)
        {
            return _speed;
        }
    }

    public void SetSpeed(int speed)
    {
        lock (_lock)
        {
            _speed = speed;
        }
    }
}
