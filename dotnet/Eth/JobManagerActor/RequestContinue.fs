namespace JobManager


[<AutoOpen>]
module RequestContinue =

  open ScrapperModels
  open ScrapperModels.JobManager
  open System.Threading.Tasks
  open Microsoft.Extensions.Logging

  type ScrapperDispatcherConfirmContiunue =
    string
      -> ScrapperDispatcher.ConfirmContinueData
      -> Task<Result<ScrapperDispatcher.ScrapperDispatcherActorResult, exn>>

  type RequestContinueEnv =
    { ScrapperDispatcherConfirmContiunue: ScrapperDispatcherConfirmContiunue }

  let requestContinue
    ((actorEnv, requestContinueEnv): ActorEnv * RequestContinueEnv)
    (data: RequestContinueData)
    : Task<Result> =
    task {

      let logger = actorEnv.Logger

      logger.LogDebug("Request continue  with {@data}")

      let! state = actorEnv.GetState()

      match state with
      | Some state ->

        logger.LogDebug("State found {@state}", state)

        let confirmData: ScrapperDispatcher.ConfirmContinueData =
          { BlockRange = data.BlockRange
            Target = data.Target }

        logger.LogDebug("Confirm data {@data}", confirmData)

        let! result = requestContinueEnv.ScrapperDispatcherConfirmContiunue data.ActorId confirmData

        logger.LogDebug("Scrapper dispatch continue {@result}", result)

        let jobId = JobId data.ActorId

        let state =
          JobResult.updateStateWithJobResult (CallChildActorData.ConfirmContinue confirmData) state (jobId, result)

        logger.LogDebug("New state {@state}", state)
        do! actorEnv.SetState state
        return state |> Ok
      | None ->
        logger.LogWarning("State not found")
        return StateNotFound |> Error

    }
