namespace ScrapperDispatcherActor

[<AutoOpen>]
module internal Failure =

  open Dapr.Actors
  open ScrapperModels
  open Microsoft.Extensions.Logging
  open Common.DaprActor

  let failure (env: ActorEnv) (data: FailureData) =
    let logger = env.Logger

    task {
      let! state = env.GetState()

      match state with
      | Some state ->
        let state =
          { state with
              Status =
                { Data = data; RetriesCount = 0u }
                |> Status.Failure
              Date = epoch () }

        logger.LogInformation("Failure with {@state}", state)

        do! env.SetState(state)

        return Some state

      | None ->
        logger.LogWarning("Failure {@failure} but state is not found", state)

        return None
    }
