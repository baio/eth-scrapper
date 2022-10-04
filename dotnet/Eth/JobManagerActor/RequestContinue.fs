namespace JobManager


[<AutoOpen>]
module RequestContinue =

  open ScrapperModels
  open ScrapperModels.JobManager
  open System.Threading.Tasks
  open Microsoft.Extensions.Logging

  let requestContinue (env: Env) (data: RequestContinueData) : Task<Result> =
    task {

      let logger = env.Logger

      logger.LogDebug("Request continue  with {@data}", data)

      let! state = env.GetState()

      match state with
      | Some state ->

        logger.LogDebug("State found {@state}", state)

        let confirmData: ScrapperDispatcher.ConfirmContinueData =
          { BlockRange = data.BlockRange
            Target = data.Target }

        logger.LogDebug("Confirm data {@data}", confirmData)

        let actor = env.CreateScrapperDispatcherActor data.ActorId

        let! result =
          actor.ConfirmContinue confirmData
          |> Common.Utils.Task.wrapException

        logger.LogDebug("Scrapper dispatch continue {@result}", result)

        let state =
          JobResult.updateStateWithJobResult
            (CallChildActorData.ConfirmContinue confirmData)
            state
            (data.ActorId, result)

        logger.LogDebug("New state {@state}", state)
        do! env.SetState state
        return state |> Ok
      | None ->
        logger.LogWarning("State not found")
        return StateNotFound |> Error

    }
