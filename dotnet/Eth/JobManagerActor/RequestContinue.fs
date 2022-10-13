namespace JobManager


[<AutoOpen>]
module RequestContinue =

  open ScrapperModels
  open ScrapperModels.JobManager
  open System.Threading.Tasks
  open Microsoft.Extensions.Logging


  let requestContinue (env: Env) (data: RequestContinueData) : Task<Result> =

    let logger = env.Logger

    task {

      use scope = logger.BeginScope("requestContinue {@data}", data)

      logger.LogDebug("Request continue")

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


        match result with
        | Error _ ->
          let state =
            JobResult.updateStateWithJobResult
              (CallChildActorData.ConfirmContinue confirmData)
              state
              (data.ActorId, result)

          logger.LogError("Call scrapper dispatcher error, updated state {@state}", state)
          do! env.SetState state
        | _ -> ()

        return state |> Ok
      | None ->
        logger.LogWarning("State not found")
        return StateNotFound |> Error

    }
