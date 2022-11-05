namespace JobManager

[<AutoOpen>]
module Resume =

  open ScrapperModels
  open ScrapperModels.JobManager
  open Microsoft.Extensions.Logging
  open System.Threading.Tasks
  open System

  let resume (env: Env) : Task<Result> =

    let logger = env.Logger

    task {
      use scope = logger.BeginScope("resume")

      logger.LogDebug("Resume")

      let! state = env.GetState()

      logger.LogDebug("Resume {@state}")

      match state with
      | Some state ->
        let jobIds = state.Jobs |> Map.keys |> List.ofSeq
        logger.LogDebug("resume jobs {@jobIds}", jobIds)

        let calls =
          jobIds
          |> List.map (fun jobId ->
            task {
              let actor = env.CreateScrapperDispatcherActor jobId
              let! result = actor.Resume() |> Common.Utils.Task.wrapException

              return (jobId, result)
            })

        // TODO : Check all
        let! result = Common.Utils.Task.all calls

        logger.LogDebug("Result for resume state", result)

        return state |> Ok
      | None ->
        logger.LogError("state not found")
        return StateNotFound |> Error
    }
