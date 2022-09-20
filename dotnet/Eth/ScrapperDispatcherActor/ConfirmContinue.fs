namespace ScrapperDispatcherActor

[<AutoOpen>]
module internal ConfirmContinue =

  open Dapr.Actors
  open ScrapperModels
  open Microsoft.Extensions.Logging
  open Common.DaprActor

  let confirmContinue ((runScrapperEnv, env): RunScrapperEnv * ActorEnv) (data: ConfirmContinueData) =

    let logger = env.Logger

    logger.LogDebug("Confirm continue with {@data}", data)

    task {
      let! state = env.GetState()

      match state with
      | Some state ->
        //if state.Request.BlockRange <> data.BlockRange || state.Target <> data.Target then
        //  logger.LogInformation("Data from previous {@state} is not the same as  {@data}, calc next request", state, data.Result)
        // TODO !
        let state = { state with Target = data.Target }
        return! runScrapper runScrapperEnv state.Request state
      | None ->
        logger.LogError("State not found")
        return StateNotFound |> Error
    }
