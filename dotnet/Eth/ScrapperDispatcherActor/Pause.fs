namespace ScrapperDispatcherActor

[<AutoOpen>]
module internal Pause =

  open Dapr.Actors
  open ScrapperModels
  open Microsoft.Extensions.Logging
  open Common.DaprActor

  let pause (env: ActorEnv) =
    task {
      let! state = env.GetState()

      match state with
      | Some state ->
        if state.Status = Status.Continue
           || state.Status = Status.Schedule then
          let state =
            { state with
                Status = Status.Pause
                Date = epoch () }

          do! env.SetState state

          return state |> Ok
        else
          let error = "Actor in a wrong state"
          env.Logger.LogDebug(error)
          return (state, error) |> StateConflict |> Error
      | None -> return StateNotFound |> Error

    }
