namespace ScrapperDispatcherActor

[<AutoOpen>]
module ScrapperDispatcherActor =

  open Dapr.Actors
  open Dapr.Actors.Runtime
  open ScrapperModels.ScrapperDispatcher
  open ScrapperModels
  open Microsoft.Extensions.Logging
  open Common.DaprActor
  open System


  let private STATE_NAME = "state"


  type ScrapperActor(proxyFactory, jobId) =
    let (JobId id) = jobId
    let proxy = createActorProxy proxyFactory (ActorId id) "ScrapperActor"

    interface ScrapperModels.Scrapper.IScrapperActor with
      member this.Scrap data =
        invokeActorProxyMethod<ScrapperRequest, bool> proxy "Scrap" data


  [<Actor(TypeName = "scrapper-dispatcher")>]
  type ScrapperDispatcherActor(host: ActorHost) as this =
    inherit Actor(host)
    let logger = ActorLogging.create host
    let stateManager = stateManager<State> STATE_NAME this.StateManager    

    let createJobManagerActor (JobManagerId id) =
      host.ProxyFactory.CreateActorProxy<JobManager.IJobManagerActor>((ActorId id), "job-manager")

    let createScrapperActor jobId =
      ScrapperActor(host.ProxyFactory, jobId) :> ScrapperModels.Scrapper.IScrapperActor

    let env: Env =
      { MaxEthItemsInResponse = 10000u
        ActorId = JobId(host.Id.ToString())
        Date = fun () -> System.DateTime.UtcNow
        GetState = stateManager.Get
        SetState = stateManager.Set
        RemoveState = stateManager.Remove
        Logger = logger
        CreateJobManagerActor = createJobManagerActor
        CreateScrapperActor = createScrapperActor
        GetEthBlocksCount = getEthBlocksCount }

    let actor = ScrapperDispatcherBaseActor env :> IScrapperDispatcherActor

    interface IScrapperDispatcherActor with

      member this.Start data = actor.Start data

      member this.Continue data = actor.Continue data

      member this.Pause() = actor.Pause()

      member this.Resume() = actor.Resume()

      member this.State() = actor.State()

      member this.Reset() = actor.Reset()

      member this.Failure data = actor.Failure data

      member this.ConfirmContinue data = actor.ConfirmContinue data
