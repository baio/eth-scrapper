namespace ScrapperTestContext

open Microsoft.Extensions.Logging
open ScrapperModels
open System.Threading.Tasks
open Common.Utils.Test
open ScrapperModels.JobManager

type ReportJobStateChanged = ScrapperModels.JobManager.State * ScrapperModels.JobManager.State -> unit

type ContextEnv =
  { EthBlocksCount: uint
    MaxEthItemsInResponse: uint
    Date: unit -> System.DateTime
    OnScrap: OnScrap
    OnReportJobStateChanged: ReportJobStateChanged option }


type JobManagerBaseActor(env, fn: ReportJobStateChanged option) =
  inherit JobManager.JobManagerBaseActor.JobManagerBaseActor(env)

  member this.ReportJobState data = base.OnReportJobState data

  override this.OnReportJobState data =
    task {
      let! pervState = (this :> IJobManagerActor).State()
      let! result = this.ReportJobState data
      let! currState = (this :> IJobManagerActor).State()

      match (pervState, currState, fn) with
      | Some pervState, Some currState, Some fn -> fn (pervState, currState)
      | _ -> ()

      return result
    }

type Context(env: ContextEnv) =

  let createLogger = Common.Logger.SerilogLogger.createDefault

  let jobManagerMap = createMapHelper<JobManagerId, JobManager.State> ()

  let managerConfigMap = createMapHelper<JobManagerId, JobManager.Config> ()

  let jobMap = createMapHelper<JobId, ScrapperDispatcher.State> ()

  member this.JobManagerMap = jobManagerMap
  member this.JobMap = jobMap

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

    { MaxEthItemsInResponse = env.MaxEthItemsInResponse
      Logger = createLogger $"job_{id}"
      ActorId = jobId
      Date = env.Date
      SetState = jobMap.AddItem jobId
      RemoveState = fun () -> jobMap.RemoveItem jobId
      GetState = fun () -> jobMap.GetItem jobId
      CreateJobManagerActor = this.createJobManager
      CreateScrapperActor = this.createScrapper
      GetEthBlocksCount = fun _ -> env.EthBlocksCount |> Task.FromResult }

  member this.createScrapperDispatcher(id: JobId) =
    id
    |> this.createScrapperDispatcherEnv
    |> ScrapperDispatcherActor.ScrapperDispatcherBaseActor.ScrapperDispatcherBaseActor
    :> ScrapperDispatcher.IScrapperDispatcherActor

  // job manager
  member this.createJobManagerEnv(jobManagerId: JobManagerId) : JobManager.Env =
    let (JobManagerId id) = jobManagerId

    let stateStore: JobManager.ActorStore<JobManager.State> =
      { Get = fun () -> jobManagerMap.GetItem jobManagerId
        Set = jobManagerMap.AddItem jobManagerId
        Remove = fun () -> jobManagerMap.RemoveItem jobManagerId }

    let configStore: JobManager.ActorStore<JobManager.Config> =
      { Get = fun () -> managerConfigMap.GetItem jobManagerId
        Set = managerConfigMap.AddItem jobManagerId
        Remove = fun () -> managerConfigMap.RemoveItem jobManagerId }

    { Logger = createLogger $"job_manager_{id}"
      ActorId = jobManagerId
      StateStore = stateStore
      ConfigStore = configStore
      CreateScrapperDispatcherActor = this.createScrapperDispatcher
      GetEthBlocksCount = fun _ -> env.EthBlocksCount |> Task.FromResult }

  member this.createJobManager(id: JobManagerId) =
    let actor =
      id
      |> this.createJobManagerEnv
      |> fun jobManagerEnv -> JobManagerBaseActor(jobManagerEnv, env.OnReportJobStateChanged)

    actor :> JobManager.IJobManagerActor

  member this.wait(scrapCnt: int) =
    Task.Delay(scrapCnt + 1) |> Async.AwaitTask
