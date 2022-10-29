namespace Dapr.Abstracts
{
    public interface IActorFactory
    {
        T CreateActor<T>(string actorId, string? actorType = null);
    }
}
