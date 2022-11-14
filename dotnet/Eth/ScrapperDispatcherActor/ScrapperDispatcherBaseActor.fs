namespace ScrapperDispatcherActor

[<AutoOpen>]
module ScrapperDispatcherBaseActor =

  open Dapr.Actors
  open ScrapperModels.ScrapperDispatcher

  type ScrapperDispatcherBaseActor(env: Env) =

    // override set state in order to report manager immediately
    let setState' parentId (state: State) =
      let manager = env.CreateJobManagerActor(parentId)

      task {
        do! env.StateStore.Set state

        manager.ReportJobState { Job = state; ActorId = env.ActorId }
        |> ignore

        return ()
      }
      :> System.Threading.Tasks.Task

    let setState (state: State) =
      match state.ParentId with
      | Some parentId -> setState' parentId state
      | None -> env.StateStore.Set state

    let env = { env with StateStore = { env.StateStore with Set = setState } }

    interface IScrapperDispatcherActor with

      member this.Start data = start env data

      member this.Continue data = continue env data

      member this.Pause() = pause env

      member this.Resume() = resume env

      member this.State() = env.StateStore.Get()

      member this.Reset() = env.StateStore.Remove()

      member this.Failure(data: FailureData) = failure env data

      member this.ConfirmContinue(data: ConfirmContinueData) = confirmContinue env data
