﻿namespace ScrapperDispatcherActor

[<AutoOpen>]
module internal Resume =

  open Dapr.Actors
  open ScrapperModels.ScrapperDispatcher
  open Microsoft.Extensions.Logging
  open Common.Utils

  let resume (env: Env) =
    let logger = env.Logger

    task {
      let! state = env.GetState()

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

          logger.LogInformation("Resume with {@pervState} {@state}", state, updatedState)

          return! runScrapper env updatedState.Request state
        | _ ->
          let error = "Actor in a wrong state"
          logger.LogDebug(error)
          return (state, error) |> StateConflict |> Error
      | _ ->
        logger.LogWarning("Resume state not found or Continue")
        return StateNotFound |> Error

    }
