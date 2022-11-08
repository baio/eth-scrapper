namespace JobManager

[<AutoOpen>]
module Start =

  open ScrapperModels
  open ScrapperModels.JobManager
  open Microsoft.Extensions.Logging
  open System.Threading.Tasks
  open System

  let private defaultState: State =
    { Jobs = Map.empty
      LatestUpdateDate = None
      Status = Initial }

  let private createChildId (JobManagerId id) idx = $"{id}_s{idx}"

  let start (env: Env) (data: StartData) : Task<Result> =

    let logger = env.Logger

    task {
      use scope = logger.BeginScope("start {actorId} {@data}", env.ActorId, data)

      logger.LogDebug("Start with {actorId} {@data}", env.ActorId, data)

      let! state = env.StateStore.Get()

      logger.LogDebug("State {@state}", state)

      match state with
      | None ->
        let! config = getConfig env
        let jobsCount = config.AvailableJobsCount
        let! blocksCount = env.GetEthBlocksCount data.EthProviderUrl //getEthBlocksCount data.EthProviderUrl
        let blockSize = Math.Ceiling(blocksCount / jobsCount) |> uint

        logger.LogDebug(
          "Data to calculate range {jobsCount} {blocksCount} {blockSize}",
          jobsCount,
          blocksCount,
          blockSize
        )

        let parentId = env.ActorId

        let rangeStartData =
          [ 0u .. (jobsCount - 1u) ]
          |> List.map (fun x -> (x * blockSize, x * blockSize + blockSize, x = jobsCount - 1u))
          |> List.map (fun (from, to', isFinal) ->
            let range = { From = from; To = to' }

            let target: ScrapperDispatcher.TargetBlockRange =
              { Range = range; ToLatest = isFinal }

            { EthProviderUrl = data.EthProviderUrl
              ContractAddress = data.ContractAddress
              Abi = data.Abi
              Target = Some target
              ParentId = Some parentId }: ScrapperDispatcher.StartData)

        logger.LogDebug("Calculated start data {@rangeStartData}", rangeStartData)

        let calls =
          rangeStartData
          |> List.mapi (fun i data ->
            let childId = createChildId parentId i
            let jobId = JobId childId

            task {
              let actor = env.CreateScrapperDispatcherActor jobId

              let! result =
                data
                |> actor.Start
                |> Common.Utils.Task.wrapException

              return jobId, result, (CallChildActorData.Start data)
            })

        let! result = Common.Utils.Task.all calls

        logger.LogDebug("Result after executing start {@result}", result)

        let state2 = JobResult.updateStateWithJobsListResult defaultState result //updateStateWithJobsListErrorResult defaultState result

        logger.LogDebug("Updated {@state} after jobs result applied", state2)

        do! env.StateStore.Set state2

        return state2 |> Ok
      //match state' with
      //| Some state ->
      //  do! env.SetState state
      //  logger.LogDebug("Updated {@state} after errors applied", state)
      //  return state |> Ok
      //| _ ->

      //  do! env.SetState defaultState
      //  logger.LogDebug("No errors, set default {@state}")
      //  return defaultState |> Ok

      | Some state ->
        logger.LogError("State already exists", state)

        return
          (state, "State already exists")
          |> StateConflict
          |> Error
    }
