namespace JobManager

[<AutoOpen>]
module Start =

  open ScrapperModels
  open ScrapperModels.JobManager
  open Microsoft.Extensions.Logging
  open System.Threading.Tasks
  open System

  let private createChildId id idx = $"{id}_s{idx}"

  type ScrapperDispatcherStart = string -> ScrapperDispatcher.StartData -> Task<Job>

  type StartEnv =
    { ScrapperDispatcherStart: ScrapperDispatcherStart }

  let start ((actorEnv, startEnv): ActorEnv * StartEnv) (actorId: string) (data: StartData) : Task<Result> =

    task {
      let! state = actorEnv.GetState()

      match state with
      | Some state ->
        let jobsCount = state.AvailableJobsCount
        let! blocksCount = getEthBlocksCount data.EthProviderUrl
        let blockSize = Math.Ceiling(blocksCount / jobsCount) |> uint

        let rangeStartData =
          [ 0u .. (blocksCount - 1u) ]
          |> List.map (fun x -> (x * blockSize, x * blockSize + blockSize, x = blocksCount - 1u))
          |> List.map (fun (from, to', isFinal) ->
            let range = { From = from; To = to' }

            let target: ScrapperDispatcher.TargetBlockRange =
              { Range = range; ToLatest = isFinal }

            let parentId = actorId

            { EthProviderUrl = data.EthProviderUrl
              ContractAddress = data.ContractAddress
              Abi = data.Abi
              Target = Some target
              ParentId = Some parentId }: ScrapperDispatcher.StartData)

        let calls =
          rangeStartData
          |> List.mapi (fun i x ->
            let childId = createChildId actorId i

            task {
              let! result = startEnv.ScrapperDispatcherStart childId x
              return (JobId childId), result
            })

        let! result = Common.Utils.Task.all calls

        let jobs = result |> Map.ofList

        let state = { state with Jobs = jobs }

        return state |> Ok
      | None -> return StateNotFound |> Error
    }
