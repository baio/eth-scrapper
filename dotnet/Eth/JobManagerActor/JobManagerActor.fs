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

  [<Actor(TypeName = "job-manager")>]
  type JobManagerActor(host: ActorHost) as this =
    inherit Actor(host)
    let logger = ActorLogging.create host
    let stateManager = stateManager<State> STATE_NAME this.StateManager

    let createScrapperDispatcherActor (JobId id) =
      host.ProxyFactory.CreateActorProxy<ScrapperDispatcher.IScrapperDispatcherActor>(
        (ActorId id),
        "scrapper-dispatcher"
      )

    let env: Env =
      { Logger = logger
        GetState = stateManager.Get
        SetState = stateManager.Set
        RemoveState = stateManager.Remove
        SetStateIfNotExist = fun state -> stateManager.AddOrUpdateState state id
        CreateScrapperDispatcherActor = createScrapperDispatcherActor
        ActorId = host.Id.ToString() |> JobManagerId
        GetEthBlocksCount = getEthBlocksCount }

    let actor' = env |> JobManagerBaseActor
    let actor = actor' :> IJobManagerActor


    interface IJobManagerActor with

      member this.Pause() : Task<Result> = actor.Pause()

      member this.Reset() : Task<Result> = actor.Reset()

      member this.Resume() : Task<Result> = actor.Resume()

      member this.SetJobsCount(count: uint) : Task<Result> = actor.SetJobsCount(count)

      member this.Start(data: StartData) : Task<Result> = actor.Start data

      member this.RequestContinue(data: RequestContinueData) : Task<Result> = actor.RequestContinue data

      member this.State() : Task<State option> = actor.State()

      member this.ReportJobState(data: JobStateData) : Task<Result> = actor.ReportJobState data
