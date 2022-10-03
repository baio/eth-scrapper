module Context

open Microsoft.Extensions.Logging
open ScrapperModels
open System.Threading.Tasks
open Common.Utils.Test

let jobManagerMap = createMapHelper<JobManagerId, JobManager.State> ()

let jobMap = createMapHelper<JobId, ScrapperDispatcher.State> ()

type OnScrap = ScrapperModels.ScrapperRequest -> ScrapperResult

let createLogger = createConsoleLogger LogLevel.Trace

type ScrapperActorEnv =
  { Logger: ILogger
    ActorId: JobId
    CreateScrapperDispatcherActor: JobId -> ScrapperModels.ScrapperDispatcher.IScrapperDispatcherActor
    CreateStoreActor: JobId -> ScrapperModels.ScrapperStore.IScrapperStoreActor
    OnScrap: OnScrap }

type ScrapperActor(env: ScrapperActorEnv) =
  interface ScrapperModels.Scrapper.IScrapperActor with
    member this.Scrap data =
      let result = env.OnScrap data

      match result with
      | Ok result ->
        let data: ScrapperModels.ScrapperStore.ContinueSuccessData =
          { EthProviderUrl = data.EthProviderUrl
            Abi = data.Abi
            ContractAddress = data.ContractAddress
            Result =
              { BlockRange = result.BlockRange
                ItemsCount = result.ItemsCount
                IndexPayload = "test payload" } }

        let actor = env.CreateStoreActor(env.ActorId)
        actor.Store data
      | Error _ ->
        let data: ScrapperModels.ScrapperDispatcher.ContinueData =
          { EthProviderUrl = data.EthProviderUrl
            Abi = data.Abi
            ContractAddress = data.ContractAddress
            Result = result }

        let actor = env.CreateScrapperDispatcherActor(env.ActorId)

        task {
          let! _ = actor.Continue data
          return true
        }



type ContextEnv =
  { Date: unit -> System.DateTime
    OnScrap: OnScrap }

type Context(env: ContextEnv) =

  // store
  member this.createScrapperEnv(jobId: JobId) : ScrapperActorEnv =
    let (JobId id) = jobId

    { Logger = createLogger $"scrapper_{id}"
      ActorId = jobId
      CreateScrapperDispatcherActor = this.createScrapperDispatcher
      CreateStoreActor = this.createStore
      OnScrap = env.OnScrap }

  member this.createScrapper(jobId: JobId) =
    jobId |> this.createScrapperEnv |> ScrapperActor :> Scrapper.IScrapperActor

  // store
  member this.createStoreEnv(jobId: JobId) : ScrapperElasticStoreActor.Env =
    let (JobId id) = jobId

    { Logger = createLogger $"store_{id}"
      ActorId = jobId
      Store = fun _ -> Task.FromResult true
      CreateScrapperDispatcherActor = this.createScrapperDispatcher }

  member this.createStore(jobId: JobId) =
    jobId
    |> this.createStoreEnv
    |> ScrapperElasticStoreActor.ScrapperStoreBaseActor.ScrapperElasticStoreBaseActor
    :> ScrapperStore.IScrapperStoreActor


  // scrapper dispatcher / job
  member this.createScrapperDispatcherEnv(jobId: JobId) : ScrapperDispatcherActor.Env =
    let (JobId id) = jobId

    { Logger = createLogger $"job_{id}"
      ActorId = jobId
      Date = env.Date
      SetState = jobMap.AddItem jobId
      RemoveState = fun () -> jobMap.RemoveItem jobId
      GetState = fun () -> jobMap.GetItem jobId
      CreateJobManagerActor = this.createJobManager
      CreateScrapperActor = this.createScrapper }

  member this.createScrapperDispatcher(id: JobId) =
    id
    |> this.createScrapperDispatcherEnv
    |> ScrapperDispatcherActor.ScrapperDispatcherBaseActor.ScrapperDispatcherBaseActor
    :> ScrapperDispatcher.IScrapperDispatcherActor

  // job manager
  member this.createJobManagerEnv(jobManagerId: JobManagerId) : JobManager.Env =
    let (JobManagerId id) = jobManagerId

    { Logger = createLogger $"job_manager_{id}"
      ActorId = jobManagerId
      SetState = jobManagerMap.AddItem jobManagerId
      SetStateIfNotExist = jobManagerMap.AddIfNotExist jobManagerId
      GetState = fun () -> jobManagerMap.GetItem jobManagerId
      CreateScrapperDispatcherActor = this.createScrapperDispatcher
      GetEthBlocksCount = fun cnt -> cnt |> System.UInt32.Parse |> Task.FromResult }

  member this.createJobManager(id: JobManagerId) =
    let actor =
      id
      |> this.createJobManagerEnv
      |> JobManager.JobManagerBaseActor.JobManagerBaseActor

    actor.Init() |> ignore
    actor :> JobManager.IJobManagerActor
