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

  let requestContinue (env: Env) (data: RequestContinueData) (state: State) =

    let logger = env.Logger

    logger.LogDebug("Request continue with {@data} {@state}", data, state)

    task {

      let actor = env.CreateJobManagerActor(state.ParentId)

      let! result = actor.RequestContinue data

      logger.LogDebug("Request continue result {@result}", result)

      match result with
      | Ok _ ->

        //let state: State =
        //  { state with
        //      Status = Status.Continue
        //      Request = { state.Request with BlockRange = data.BlockRange }
        //      Target = data.Target
        //      Date = (env.Date() |> toEpoch) }

        //do! env.SetState state

        return state |> Ok
      | Error _ ->
        let state: State =
          { state with
              Status =
                { Data =
                    { AppId = AppId.Dispatcher
                      Status = AppId.JobManager |> CallChildActorFailure }
                  FailuresCount = 0u }
                |> Status.Failure
              Request = { state.Request with BlockRange = data.BlockRange }
              Target = data.Target
              Date = env.Date() |> toEpoch }

        do! env.SetState state

        return state |> ActorFailure |> Error
    }
