namespace JobManager

[<AutoOpen>]
module SetJobsCount =

  open ScrapperModels.JobManager
  open Microsoft.Extensions.Logging
  open System.Threading.Tasks

  let setJobsCount (env: ActorEnv) (count: uint) : Task<Result> =

    let logger = env.Logger

    logger.LogDebug("SetJobsCount {count}", count)

    task {
      let! state = env.GetState()

      match state with
      | Some state ->
        logger.LogDebug("State found {state}", state)

        if count = 0u && count > 10u then
          logger.LogDebug("Attemt to set wrong jobs count {count}", count)

          return
            "Wrong jobs count, must be in the range of [1, 10]"
            |> ValidationFailure
            |> Error
        else
          let state = { state with AvailableJobsCount = count }
          do! env.SetState state
          logger.LogDebug("Jobs count updated {state}", state)
          return state |> Ok
      | None ->
        logger.LogError("State not found")
        return StateNotFound |> Error
    }
