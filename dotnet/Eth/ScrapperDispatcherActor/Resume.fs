namespace ScrapperDispatcherActor

[<AutoOpen>]
module internal Resume =

  open Dapr.Actors
  open Dapr.Actors.Runtime
  open System.Threading.Tasks
  open ScrapperModels
  open Microsoft.Extensions.Logging
  open Common.DaprActor
  open Common.DaprActor.ActorResult
  open System
  open Nethereum.Web3

  let resume ((runScrapperEnv, env): RunScrapperEnv * ActorEnv) =
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
                Date = epoch () }

          logger.LogInformation("Resume with {@pervState} {@state}", state, updatedState)

          return! runScrapper runScrapperEnv env.ActorId (Continue state) updatedState.Request
        | _ ->
          let error = "Actor in a wrong state"
          logger.LogDebug(error)
          return (state, error) |> StateConflict |> Error
      | _ ->
        logger.LogWarning("Resume state not found or Continue")
        return StateNotFound |> Error

    }
