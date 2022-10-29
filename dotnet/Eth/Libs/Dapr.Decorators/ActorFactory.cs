using Dapr.Abstracts;
using Dapr.Actors;
using Dapr.Actors.Client;
using Dapr.Actors.Runtime;
using Dapr.Client;
using System.Reflection;

namespace Dapr.Decorators
{
    public class ActorFactory : IActorFactory
    {

        public ActorFactory(IActorProxyFactory actorProxyFactory) {
            ActorProxyFactory = actorProxyFactory;
        }

        public IActorProxyFactory ActorProxyFactory { get; }

        public T CreateActor<T>(string actorId, string? actorType = null) 
        {
            var id = new ActorId(actorId);
            string type = actorType ?? typeof(T).GetCustomAttribute<ActorAttribute>().TypeName;

            return (T)ActorProxyFactory.CreateActorProxy(id, typeof(T), type);
        }
    }
}
