namespace JobManager

[<AutoOpen>]
module JobManagerBaseActor =

  open Dapr.Actors
  open Dapr.Actors.Runtime
  open ScrapperModels
  open Microsoft.Extensions.Logging
  open Common.DaprActor
  open System
  open ScrapperModels.JobManager
  open System.Threading.Tasks

  type JobManagerBaseActor(env: Env) =

    interface IJobManagerActor with

      member this.Pause() : Task<Result> = pause env

      member this.Reset() : Task<Result> = reset env

      member this.Resume() : Task<Result> = resume env

      member this.SetJobsCount(count: uint) : Task<Result<Config, string>> = setJobsCount env count

      member this.Start(data: StartData) : Task<Result> = start env data

      member this.RequestContinue(data: RequestContinueData) : Task<Result> = requestContinue env data

      member this.State() : Task<State option> = env.StateStore.Get()

      member this.ReportJobState(data: JobStateData) : Task<Result> = reportJobState env data

      member this.Config() : Task<Config> = getConfig env
