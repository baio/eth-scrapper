namespace JobManager

[<AutoOpen>]
module ReportJobState =
  open ScrapperModels.JobManager
  open ScrapperModels
  open ScrapperModels.JobManager
  open Microsoft.Extensions.Logging
  open System.Threading.Tasks
  open System

  let reportJobState (env: Env) (data: JobStateData) =
    let logger = env.Logger

    task {
      use scope = logger.BeginScope("reportJobState {@data}", data)
      logger.LogDebug("reportJobState")
      let! state = env.StateStore.Get()
      logger.LogDebug("Previous state: {@state}", state)

      match state with
      | Some state ->
        let jobId = data.ActorId

        let state2 = JobResult.updateStateWithJob state (jobId, data.Job)
        logger.LogDebug("Update state with job: {@state}", state2)
        do! env.StateStore.Set state2
        logger.LogDebug("State updated {@state}", state2)
        return state2 |> Ok
      | None ->
        logger.LogError("State not found")
        return StateNotFound |> Error
    }
