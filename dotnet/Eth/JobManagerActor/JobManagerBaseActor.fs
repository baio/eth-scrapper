namespace JobManager

[<AutoOpen>]
module JobManagerBaseActor =

  open Dapr.Actors
  open Dapr.Actors.Runtime
  open ScrapperModels
  open Microsoft.Extensions.Logging
  open Common.DaprActor
  open System
  open ScrapperModels.ScrapperDispatcher
  open ScrapperModels.JobManager
  open System.Threading.Tasks


  let private defaultState: State =
    { AvailableJobsCount = 1u
      Jobs = Map.empty
      LatestUpdateDate = None
      Status = Initial }

  type JobManagerBaseActor(env: Env) =

    member this.Init() = env.SetStateIfNotExist defaultState

    interface IJobManagerActor with

      member this.Pause() : Task<Result> =
        raise (System.NotImplementedException())

      member this.Reset() : Task<Result> = reset env defaultState

      member this.Resume() : Task<Result> =
        raise (System.NotImplementedException())

      member this.SetJobsCount(count: uint) : Task<Result> = setJobsCount env count

      member this.Start(data: StartData) : Task<Result> = start env data

      member this.RequestContinue(data: RequestContinueData) : Task<Result> = requestContinue env data

      member this.State() : Task<State option> = env.GetState()

      member this.ReportJobState(data: JobStateData) : Task<Result> = reportJobState env data
