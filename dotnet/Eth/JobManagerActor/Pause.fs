namespace JobManager

[<AutoOpen>]
module Pause =

  open ScrapperModels
  open ScrapperModels.JobManager
  open Microsoft.Extensions.Logging
  open System.Threading.Tasks
  open System

  let pause (env: Env) : Task<Result> =

    let logger = env.Logger

    task {
      use scope = logger.BeginScope("pause")

      logger.LogDebug("Pause")

      let! state = env.StateStore.Get()

      logger.LogDebug("State {@state}")

      match state with
      | Some state ->
        let jobIds = state.Jobs |> Map.keys |> List.ofSeq
        logger.LogDebug("pause jobs {@jobIds}", jobIds)

        let calls =
          jobIds
          |> List.map (fun jobId ->
            task {
              let actor = env.CreateScrapperDispatcherActor jobId
              let! result = actor.Pause() |> Common.Utils.Task.wrapException

              return (jobId, result)
            })

        // TODO : Check all 
        let! result = Common.Utils.Task.all calls

        logger.LogDebug("Result for pause state", result)

        return state |> Ok
      | None ->
        logger.LogError("state not found")
        return StateNotFound |> Error
    }
