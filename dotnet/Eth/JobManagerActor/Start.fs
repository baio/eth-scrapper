﻿namespace JobManager

[<AutoOpen>]
module Start =

  open ScrapperModels
  open ScrapperModels.JobManager
  open Microsoft.Extensions.Logging
  open System.Threading.Tasks
  open System

  let private createChildId id idx = $"{id}_s{idx}"

  let start (env: Env) (data: StartData) : Task<Result> =

    let logger = env.Logger
    logger.LogDebug("start with {actorId} {@data}", env.ActorId, data)

    task {
      let! state = env.GetState()

      logger.LogDebug("state {@state}", state)

      match state with
      | Some state ->
        let jobsCount = state.AvailableJobsCount
        let! blocksCount = getEthBlocksCount data.EthProviderUrl
        let blockSize = Math.Ceiling(blocksCount / jobsCount) |> uint

        logger.LogDebug(
          "data to calculate range {jobsCount} {blocksCount} {blockSize}",
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
              ParentId = parentId }: ScrapperDispatcher.StartData)

        logger.LogDebug("calculated start data {@rangeStartData}", rangeStartData)

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

        let state = JobResult.updateStateWithJobsListResult state result

        do! env.SetState state

        logger.LogDebug("updated  {@state}", state)

        return state |> Ok
      | None ->
        logger.LogError("state not found")
        return StateNotFound |> Error
    }