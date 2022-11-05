namespace ScrapperDispatcherActor

[<AutoOpen>]
module internal RequestContinue =
  open Dapr.Actors
  open System.Threading.Tasks
  open ScrapperModels.JobManager
  open ScrapperModels.ScrapperDispatcher
  open Microsoft.Extensions.Logging
  open Common.Utils
  open ScrapperModels

  let requestContinue (env: Env) (parentId: JobManagerId) (data: RequestContinueData) (state: State) =

    let logger = env.Logger

    task {

      use scope = logger.BeginScope("requestContinue {@data} {@state}", data, state)
      logger.LogDebug("Request continue")

      let state: State =
        { state with
            Status = Status.Continue
            Request = { state.Request with BlockRange = data.BlockRange }
            Target = data.Target
            Date = (env.Date() |> toEpoch) }

      do! env.SetState state

      let actor = env.CreateJobManagerActor(parentId)

      // TODO
      actor.RequestContinue data |> ignore

      return state |> Ok
    //logger.LogDebug("Request continue result {@result}", result)

    //match result with
    //| Ok _ ->

    //  return state |> Ok
    //| Error _ ->
    //  let state: State =
    //    { state with
    //        Status =
    //          { Data =
    //              { AppId = AppId.Dispatcher
    //                Status = AppId.JobManager |> CallChildActorFailure }
    //            FailuresCount = 0u }
    //          |> Status.Failure
    //        Request = { state.Request with BlockRange = data.BlockRange }
    //        Target = data.Target
    //        Date = env.Date() |> toEpoch }

    //  do! env.SetState state

    //  return state |> ActorFailure |> Error
    }
