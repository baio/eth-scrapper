namespace ScrapperDispatcherActor

[<AutoOpen>]
module internal RequestContinue =
  open Dapr.Actors
  open System.Threading.Tasks
  open ScrapperModels.JobManager
  open ScrapperModels.ScrapperDispatcher
  open Microsoft.Extensions.Logging
  open Common.DaprActor

  type RequestContinueEnv =
    { InvokeActor: RequestContinueData -> Task<Result<unit, exn>>
      Logger: ILogger
      SetState: State -> Task }

  let requestContinue
    ({ InvokeActor = invokeActor
       Logger = logger
       SetState = setState }: RequestContinueEnv)
    (data: RequestContinueData)
    (state: State)
    =

    logger.LogDebug("Request continue with {@data} {@state}", data, state)

    task {
      let! result = invokeActor data

      logger.LogDebug("Request continue result {@result}", result)

      match result with
      | Ok _ ->

        let state: State =
          { state with
              Status = Status.Continue
              Request = { state.Request with BlockRange = data.BlockRange }
              Date = epoch () }

        do! setState state

        return state |> Ok
      | Error _ ->
        let state: State =
          { state with
              Status =
                // TODO !
                { Data =
                    { AppId = AppId.Dispatcher
                      Status = AppId.JobManager |> CallChildActorFailure }
                  FailuresCount = 0u }
                |> Status.Failure
              Request = { state.Request with BlockRange = data.BlockRange }
              Date = epoch () }

        do! setState state

        return state |> ActorFailure |> Error
    }
