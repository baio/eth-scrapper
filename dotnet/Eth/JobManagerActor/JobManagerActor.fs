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

  let private defaultState: State =
    { AvailableJobsCount = 1u
      Jobs = Map.empty
      Status = Initial }

  [<Actor(TypeName = "job-manager")>]
  type JobManagerActor(host: ActorHost) as this =
    inherit Actor(host)
    let logger = ActorLogging.create host
    let stateManager = stateManager<State> STATE_NAME this.StateManager

    let createDispatcherActor id =
      host.ProxyFactory.CreateActorProxy<ScrapperDispatcher.IScrapperDispatcherActor>(
        (ActorId id),
        "scrapper-dispatcher"
      )

    let actorEnv: ActorEnv =
      { Logger = logger
        SetState = stateManager.Set
        GetState = stateManager.Get }

    let startEnv: StartEnv =
      { ScrapperDispatcherStart =
          fun id data ->
            let actor = createDispatcherActor id
            actor.Start(data) |> ActorResult.wrapException }

    let requestContinueEnv: RequestContinueEnv =
      { ScrapperDispatcherConfirmContiunue =
          fun id data ->
            let actor = createDispatcherActor id

            actor.ConfirmContinue(data)
            |> ActorResult.wrapException }

    override this.OnActivateAsync() =
      stateManager.AddOrUpdateState defaultState id

    interface IJobManagerActor with

      member this.Pause() : Task<Result> =
        raise (System.NotImplementedException())

      member this.Reset() : Task<Result> =
        task {
          let! _ = stateManager.Remove()
          let! state = stateManager.AddOrUpdateState defaultState id
          return state |> Ok
        }

      member this.Resume() : Task<Result> =
        raise (System.NotImplementedException())

      member this.SetJobsCount(count: uint) : Task<Result> = setJobsCount actorEnv count

      member this.Start(data: StartData) : Task<Result> =
        start (actorEnv, startEnv) (host.Id.ToString()) data

      member this.RequestContinue(data: RequestContinueData) : Task<Result> =
        requestContinue (actorEnv, requestContinueEnv) data

      member this.State() : Task<State option> = stateManager.Get()
