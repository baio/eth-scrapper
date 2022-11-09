namespace ScrapperTestContext

open ScrapperModels
open ScrapperModels.JobManager
open System.Threading.Tasks
open System.Threading
open Microsoft.Extensions.Logging

type ReportJobStateChanged = ScrapperModels.JobManager.State * ScrapperModels.JobManager.State -> unit

type JobManagerActor(env, fn: ReportJobStateChanged option) as this =
  let actor =
    JobManager.JobManagerBaseActor.JobManagerBaseActor(env) :> IJobManagerActor

  let lock' fn =
    task { return lock this (fun () -> fn ()) }

  let lockt (fn: 'a -> Task<_>) (x: 'a) =
    task { return lock this (fun () -> (fn x).Result) }

  interface IJobManagerActor with

    member this.Pause() : Task<Result> = lockt actor.Pause ()

    member this.Reset() : Task<Result> = lockt actor.Reset ()

    member this.Resume() : Task<Result> = lockt actor.Resume ()

    member this.SetJobsCount(count: uint) : Task<Result<Config, string>> = lockt actor.SetJobsCount count

    member this.Start(data: StartData) : Task<Result> = lockt actor.Start data

    member this.RequestContinue(data: RequestContinueData) : Task<Result> = lockt actor.RequestContinue data

    member this.State() : Task<State option> = actor.State()

    member this.ReportJobState(data: JobStateData) : Task<Result> =
      lock' (fun () ->
        let pervState = actor.State().Result
        let result = actor.ReportJobState(data).Result
        let currState = actor.State().Result

        match pervState, currState, fn with
        | Some pervState, Some currState, Some fn -> fn (pervState, currState)
        | _ -> ()

        result)



    member this.Config() : Task<Config> = lockt actor.Config ()
