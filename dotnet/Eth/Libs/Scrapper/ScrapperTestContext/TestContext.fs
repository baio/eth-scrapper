namespace ScrapperTestContext

open ScrapperModels
open System.Threading.Tasks
open Common.Utils.Test
open ScrapperModels.JobManager
open ScrapperModels.ScrapperDispatcher

type ContextEnv =
  { EthBlocksCount: uint
    MaxEthItemsInResponse: uint
    Date: unit -> System.DateTime
    OnScrap: OnScrap
    MailboxHooks: MailboxHooks
    OnReportJobStateChanged: ReportJobStateChanged option }

module private Helpers =

  let createActorFactory<'k, 'v when 'k: comparison> fn =
    let mutable map = Map.empty<'k, 'v>

    fun id ->
      let actor = Map.tryFind id map

      match actor with
      | Some actor -> actor
      | None ->
        let actor = id |> fn
        map <- map.Add(id, actor)
        actor

  let withHooks<'a> (env: ContextEnv) (x: 'a) = x, env.MailboxHooks

type Context(env: ContextEnv) as this =

  let jobManagersFactory =
    Helpers.createActorFactory<JobManagerId, IJobManagerActor> (fun id ->
      id
      |> this.createJobManagerEnv
      |> fun jobManagerEnv -> JobManagerActor(jobManagerEnv, env.MailboxHooks, env.OnReportJobStateChanged)
      :> JobManager.IJobManagerActor)

  let jobsFactory =
    Helpers.createActorFactory<JobId, IScrapperDispatcherActor> (fun id ->
      id
      |> this.createScrapperDispatcherEnv
      |> Helpers.withHooks env
      |> JobActor
      :> ScrapperDispatcher.IScrapperDispatcherActor)

  let scrappersFactory =
    Helpers.createActorFactory<JobId, Scrapper.IScrapperActor> (fun id ->
      id |> this.createScrapperEnv |> ScrapperActor :> Scrapper.IScrapperActor)

  let storesFactory =
    Helpers.createActorFactory<JobId, ScrapperStore.IScrapperStoreActor> (fun id ->
      id |> this.createStoreEnv |> StoreActor :> ScrapperStore.IScrapperStoreActor)

  let createLogger = Common.Logger.SerilogLogger.createDefault

  let jobManagerStateMap = createMapHelper<JobManagerId, JobManager.State> ()

  let jobStateMap = createMapHelper<JobId, ScrapperDispatcher.State> ()

  let managerConfigMap = createMapHelper<JobManagerId, JobManager.Config> ()

  member this.JobManagerStateMap = jobManagerStateMap
  member this.JobStateMap = jobStateMap

  // store
  member this.createScrapperEnv(jobId: JobId) : ScrapperActorEnv =
    let (JobId id) = jobId

    { Logger = createLogger $"scrapper_{id}"
      ActorId = jobId
      CreateScrapperDispatcherActor = this.createScrapperDispatcher
      CreateStoreActor = this.createStore
      OnScrap = env.OnScrap }

  member this.createScrapper = scrappersFactory

  // store
  member this.createStoreEnv(jobId: JobId) : ScrapperElasticStoreActor.Env =
    let (JobId id) = jobId

    { Logger = createLogger $"store_{id}"
      ActorId = jobId
      Store = fun _ -> Task.FromResult true
      CreateScrapperDispatcherActor = this.createScrapperDispatcher }

  member this.createStore = storesFactory


  // scrapper dispatcher / job
  member this.createScrapperDispatcherEnv(jobId: JobId) : ScrapperDispatcherActor.Env =

    let stateStore: ActorStore<ScrapperDispatcher.State> =
      { Get = fun () -> jobStateMap.GetItem jobId
        Set = jobStateMap.AddItem jobId
        Remove = fun () -> jobStateMap.RemoveItem jobId }

    { MaxEthItemsInResponse = env.MaxEthItemsInResponse
      Logger = createLogger $"job_{id}"
      ActorId = jobId
      Date = env.Date
      StateStore = stateStore
      CreateJobManagerActor = this.createJobManager
      CreateScrapperActor = this.createScrapper
      GetEthBlocksCount = fun _ -> env.EthBlocksCount |> Task.FromResult }

  member this.createScrapperDispatcher = jobsFactory

  // job manager
  member this.createJobManagerEnv(jobManagerId: JobManagerId) : JobManager.Env =
    let (JobManagerId id) = jobManagerId

    let stateStore: ActorStore<JobManager.State> =
      { Get = fun () -> jobManagerStateMap.GetItem jobManagerId
        Set = jobManagerStateMap.AddItem jobManagerId
        Remove = fun () -> jobManagerStateMap.RemoveItem jobManagerId }

    let configStore: ActorStore<JobManager.Config> =
      { Get = fun () -> managerConfigMap.GetItem jobManagerId
        Set = managerConfigMap.AddItem jobManagerId
        Remove = fun () -> managerConfigMap.RemoveItem jobManagerId }

    { Logger = createLogger $"job_manager_{id}"
      ActorId = jobManagerId
      StateStore = stateStore
      ConfigStore = configStore
      CreateScrapperDispatcherActor = this.createScrapperDispatcher
      GetEthBlocksCount = fun _ -> env.EthBlocksCount |> Task.FromResult }

  member this.createJobManager = jobManagersFactory
