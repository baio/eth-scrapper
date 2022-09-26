namespace JobManager

[<AutoOpen>]
module JobManagerActor =

  open Dapr.Actors
  open Dapr.Actors.Runtime
  open ScrapperModels
  open Microsoft.Extensions.Logging
  open Common.DaprActor
  open System
  open ScrapperModels.JobManager
  open System.Threading.Tasks

  let private STATE_NAME = "state"

  let private defaultState: State = { AvailableJobsCount = 1u; Jobs = [] }

  [<Actor(TypeName = "job-manager")>]
  type JobManagerActor(host: ActorHost) as this =
    inherit Actor(host)
    let logger = ActorLogging.create host
    let stateManager = stateManager<State> STATE_NAME this.StateManager

    interface IJobManagerActor with
      member this.SetJobsCount(count: uint) : Task<Result> =
        logger.LogDebug("SetJobsCount {count}", count)

        task {
          if count = 0u && count > 10u then
            logger.LogDebug("Attemt to set wrong jobs count {count}", count)

            return
              "Wrong jobs count, must be in the range of [1, 10]"
              |> ValidationFailure
              |> Error
          else
            let! state = stateManager.UpdateState defaultState (fun x -> { x with AvailableJobsCount = count })
            logger.LogDebug("Jobs count updated {state}", state)
            return state |> Ok
        }

      member this.Start(data: StartData) : Task<Result> =
        task {
          let! state = stateManager.Get()

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

                { EthProviderUrl = data.EthProviderUrl
                  ContractAddress = data.ContractAddress
                  Abi = data.Abi
                  Target = Some target
                  ParentId = (Some(this.Id.ToString())) }: ScrapperDispatcher.StartData)

            let calls =
              rangeStartData
              |> List.mapi (fun i x ->
                let childId = ActorId($"{this.Id}_s{i}")

                let actor =
                  host.ProxyFactory.CreateActorProxy<ScrapperDispatcher.IScrapperDispatcherActor>(
                    childId,
                    "scrapper-dispatcher"
                  )
                actor.Start(x))

            let! result = Common.Utils.Task.all calls
            
            return state |> Ok
          | None -> return StateNotFound |> Error
        }

      member this.RequestContinue(data: RequestContinueData) : Task<Result> =
        raise (System.NotImplementedException())
