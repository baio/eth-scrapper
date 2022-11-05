namespace JobManager

[<AutoOpen>]
module Reset =

  open ScrapperModels
  open ScrapperModels.JobManager
  open Microsoft.Extensions.Logging
  open System.Threading.Tasks
  open System

  let reset (env: Env) : Task<Result> =

    let logger = env.Logger

    task {

      
      use scope = logger.BeginScope("reset")

      logger.LogDebug("Reset")

      let! state = env.GetState()

      logger.LogDebug("State {@state}")

      match state with
      | Some state ->
        let jobIds = state.Jobs |> Map.keys |> List.ofSeq
        logger.LogDebug("reset jobs {@jobIds}", jobIds)

        let calls =
          jobIds
          |> List.map (fun jobId ->
            task {
              let actor = env.CreateScrapperDispatcherActor jobId
              let! result = actor.Reset() |> Common.Utils.Task.wrapException

              return (jobId, result)
            })

        // TODO : Check all 
        let! result = Common.Utils.Task.all calls

        logger.LogDebug("Result for reset state", result)

        let! _ = env.RemoveState()

        logger.LogDebug("State removed")

        return state |> Ok
      | None ->
        logger.LogError("state not found")
        return StateNotFound |> Error
    }
