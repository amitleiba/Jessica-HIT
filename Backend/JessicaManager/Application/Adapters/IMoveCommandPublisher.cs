namespace JessicaManager.Application.Adapters;

public interface IMoveCommandPublisher
{
    Task PublishMoveCommandAsync(int leftWheel, int rightWheel, CancellationToken cancellationToken);
}
