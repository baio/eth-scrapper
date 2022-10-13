namespace Common.DaprActor

open Dapr.Actors.Client

[<AutoOpen>]
module ActorRunner =
  open Dapr.Actors
  open Dapr.Actors.Runtime

  let createActorProxy (proxyFactory: Client.IActorProxyFactory) actorId (actorType: string) =
    proxyFactory.Create(actorId, actorType)

  let invokeActorProxyMethod<'a, 'r> (actor: ActorProxy) (methodName: string) (data: 'a) =
    actor.InvokeMethodAsync<'a, 'r>(methodName, data)

  let invokeActor<'a, 'r>
    (proxyFactory: Client.IActorProxyFactory)
    actorId
    (actorType: string)
    (methodName: string)
    (data: 'a)
    =
    let actor = createActorProxy proxyFactory actorId actorType

    invokeActorProxyMethod actor methodName data
    |> Common.Utils.Task.wrapException


  let invokeActorId<'a, 'r> (actorHost: ActorHost) actorType methodName (data: 'a) =
    invokeActor actorHost.ProxyFactory actorHost.Id actorType methodName data
