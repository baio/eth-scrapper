namespace Common.DaprActor

[<AutoOpen>]
module ActorState =
  open Dapr.Actors.Runtime

  let getState<'a> (name: string) (stateManager: IActorStateManager) =
    task {
      let! result = stateManager.TryGetStateAsync<'a> name

      return
        match result.HasValue with
        | true -> Some result.Value
        | false -> None
    }

  let setState<'a> (name: string) (stateManager: IActorStateManager) (state: 'a) =
    stateManager.SetStateAsync(name, state)

  let removeState (name: string) (stateManager: IActorStateManager) = stateManager.TryRemoveStateAsync(name)

  let stateManager<'a> (name: string) (stateManager: IActorStateManager) =
    {| Get = fun () -> getState<'a> name stateManager
       Set = setState<'a> name stateManager
       Remove = fun () -> removeState name stateManager |}
