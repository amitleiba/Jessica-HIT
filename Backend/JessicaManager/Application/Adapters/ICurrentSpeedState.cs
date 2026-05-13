namespace JessicaManager.Application.Adapters;

/// <summary>
/// Maintains the current desired speed of the robot.
/// Used to decouple speed settings from direction commands.
/// </summary>
public interface ICurrentSpeedState
{
    int GetSpeed();
    void SetSpeed(int speed);
}
