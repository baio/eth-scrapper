namespace ScrapperDispatcherActor

[<AutoOpen>]
module internal Pause =

  open Dapr.Actors
  open ScrapperModels.ScrapperDispatcher
  open Microsoft.Extensions.Logging
  open Common.Utils

  let pause (env: Env) =

    let logger = env.Logger

    task {

      use scope = logger.BeginScope("pause")

      logger.LogDebug("Pause")

      let! state = env.StateStore.Get()

      match state with
      | Some state ->
        if state.Status = Status.Continue
           || state.Status = Status.Schedule then
          let state2 =
            { state with
                Status = Status.Pause
                Date = (env.Date() |> toEpoch) }

          do! env.StateStore.Set state2

          return state2 |> Ok
        else
          logger.LogDebug("Actor in a wrong {@state}", state)

          return
            (state, "Actor in a wrong state")
            |> StateConflict
            |> Error
      | None -> return StateNotFound |> Error

    }
