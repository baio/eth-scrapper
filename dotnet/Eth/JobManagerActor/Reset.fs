namespace JobManager

[<AutoOpen>]
module Reset =

  open ScrapperModels
  open ScrapperModels.JobManager
  open Microsoft.Extensions.Logging
  open System.Threading.Tasks
  open System


  type ScrapperDispatcherReset = JobId -> Task<Result<bool, exn>>

  type ResetEnv =
    { ScrapperDispatcherReset: ScrapperDispatcherReset }

  let reset ((actorEnv, resetEnv): ActorEnv * ResetEnv) (defaultState: State) : Task<Result> =

    let logger = actorEnv.Logger
    logger.LogDebug("Reset")

    task {
      let! state = actorEnv.GetState()

      logger.LogDebug("State {@state}")

      match state with
      | Some state ->
        let jobIds = state.Jobs |> Map.keys |> List.ofSeq
        logger.LogDebug("reset jobs {@jobIds}", jobIds)

        let calls =
          jobIds
          |> List.map (fun jobId ->
            task {
              let! result = resetEnv.ScrapperDispatcherReset jobId
              return (jobId, result)
            })

        let! result = Common.Utils.Task.all calls

        logger.LogDebug("Result for reset state", result)

        let state = defaultState

        do! actorEnv.SetState state

        logger.LogDebug("updated  {@state}", state)

        return state |> Ok
      | None ->
        logger.LogError("state not found")
        return StateNotFound |> Error
    }
