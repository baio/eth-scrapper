namespace JobManager

[<AutoOpen>]
module Start =

  open ScrapperModels
  open ScrapperModels.JobManager
  open Microsoft.Extensions.Logging
  open System.Threading.Tasks
  open System

  let private createChildId id idx = $"{id}_s{idx}"

  type ScrapperDispatcherStart =
    string -> ScrapperDispatcher.StartData -> Task<Result<ScrapperDispatcher.ScrapperDispatcherActorResult, exn>>

  type StartEnv =
    { ScrapperDispatcherStart: ScrapperDispatcherStart }



  let start ((actorEnv, startEnv): ActorEnv * StartEnv) (actorId: string) (data: StartData) : Task<Result> =

    let logger = actorEnv.Logger
    logger.LogDebug("start with {actorId} {@data}", actorId, data)

    task {
      let! state = actorEnv.GetState()

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

        let rangeStartData =
          [ 0u .. (jobsCount - 1u) ]
          |> List.map (fun x -> (x * blockSize, x * blockSize + blockSize, x = jobsCount - 1u))
          |> List.map (fun (from, to', isFinal) ->
            let range = { From = from; To = to' }

            let target: ScrapperDispatcher.TargetBlockRange =
              { Range = range; ToLatest = isFinal }

            let parentId = actorId

            { EthProviderUrl = data.EthProviderUrl
              ContractAddress = data.ContractAddress
              Abi = data.Abi
              Target = Some target
              ParentId = parentId }: ScrapperDispatcher.StartData)

        logger.LogDebug("calculated start data {@rangeStartData}", rangeStartData)

        let calls =
          rangeStartData
          |> List.mapi (fun i x ->
            let childId = createChildId actorId i

            task {
              let! result = startEnv.ScrapperDispatcherStart childId x
              return (JobId childId), result
            })

        let! result = Common.Utils.Task.all calls

        let state =
          JobResult.updateStateWithJobsListResult ChildActorMethodName.Start state result

        logger.LogDebug("updated  {@state}", state)

        return state |> Ok
      | None ->
        logger.LogError("state not found")
        return StateNotFound |> Error
    }
