namespace ScrapperDispatcherActor

[<AutoOpen>]
module internal Resume =

  open Dapr.Actors
  open ScrapperModels.ScrapperDispatcher
  open Microsoft.Extensions.Logging
  open Common.Utils

  let resume (env: Env) =
    let logger = env.Logger

    task {

      use scope = logger.BeginScope("resume")

      logger.LogDebug("Resume")

      let! state = env.StateStore.Get()

      match state with
      | Some state ->
        match state.Status with
        | Status.Pause
        | Status.Finish
        | Status.Failure _ ->

          let updatedState =
            { state with
                Status = Status.Continue
                Date = (env.Date() |> toEpoch) }

          logger.LogDebug("Resume with {@pervState} {@state}", state, updatedState)

          return! runScrapper env updatedState.Request updatedState
        | _ ->
          logger.LogDebug("Actor in a wrong {@state}", state)

          return
            (state, "Actor in a wrong state")
            |> StateConflict
            |> Error
      | _ ->
        logger.LogWarning("Resume state not found or Continue")
        return StateNotFound |> Error

    }
