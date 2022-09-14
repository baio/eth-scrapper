namespace Common.DaprActor

[<AutoOpen>]
module ActorRunner =
  open Dapr.Actors
  open Dapr.Actors.Runtime

  let invokeActor<'a, 'r>
    (proxyFactory: Client.IActorProxyFactory)
    actorId
    (actorType: string)
    (methodName: string)
    (data: 'a)
    =
    task {

      try
        let actor = proxyFactory.Create(actorId, actorType)

        let! _ = actor.InvokeMethodAsync<'a, 'r>(methodName, data)

        return Ok()
      with
      | _ as ex -> return Error(ex)
    }

  let invokeActorId<'a, 'r> (actorHost: ActorHost) actorType methodName (data: 'a) =
    invokeActor actorHost.ProxyFactory actorHost.Id actorType methodName data
