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
    logger.LogDebug("Report job state {@data}", data)

    task {
      let! state = env.GetState()
      logger.LogDebug("Previous state: {@state}", state)

      match state with
      | Some state ->
        let jobId = data.ActorId

        let state = JobResult.updateStateWithJob state (jobId, data.Job)
        do! env.SetState state
        return state |> Ok
      | None ->
        logger.LogError("State not found")
        return StateNotFound |> Error
    }
