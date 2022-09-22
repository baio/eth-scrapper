namespace ScrapperDispatcherActor

[<AutoOpen>]
module internal Failure =

  open Dapr.Actors
  open ScrapperModels.ScrapperDispatcherActor
  open Microsoft.Extensions.Logging
  open Common.DaprActor

  let MAX_RETRIES_COUNT = 3u

  let failure ((runScrapperEnv, env): RunScrapperEnv * ActorEnv) (data: FailureData) =
    let logger = env.Logger

    task {
      let! state = env.GetState()

      match state with
      | Some state ->
        let failuresCount =
          match state.Status with
          | Status.Failure failure -> failure.FailuresCount
          | _ -> 0u

        let state =
          { state with
              Status =
                { Data = data
                  FailuresCount = failuresCount + 1u }
                |> Status.Failure
              Date = epoch () }

        do! env.SetState(state)

        if failuresCount < MAX_RETRIES_COUNT then
          logger.LogWarning("Retriable failure with {@state}", state)
          return! runScrapper runScrapperEnv state.Request state
        else
          logger.LogError("Final failure with {@state}", state)

          return state |> Ok

      | None ->
        logger.LogWarning("Failure {@failure} but state is not found", state)

        return StateNotFound |> Error
    }
