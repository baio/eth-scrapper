namespace ScrapperTestContext

open ScrapperModels
open ScrapperModels.JobManager
open System.Threading.Tasks

type ReportJobStateChanged = ScrapperModels.JobManager.State * ScrapperModels.JobManager.State -> unit

type JobManagerActor(env, fn: ReportJobStateChanged option) =

  let mailbox = createMailbox ()

  let actor =
    JobManager.JobManagerBaseActor.JobManagerBaseActor(env) :> IJobManagerActor

  let reportJobState (data: JobStateData) =
    task {
      let! pervState = actor.State()
      let! result = actor.ReportJobState data
      let! currState = actor.State()

      match pervState, currState, fn with
      | Some pervState, Some currState, Some fn -> fn (pervState, currState)
      | _ -> ()

      return result
    }

  interface IJobManagerActor with

    member this.Pause() : Task<Result> = sync mailbox <@ actor.Pause @> ()

    member this.Reset() : Task<Result> = sync mailbox <@ actor.Reset @> ()

    member this.Resume() : Task<Result> = sync mailbox <@ actor.Resume @> ()

    member this.SetJobsCount(count: uint) : Task<Result<Config, string>> =
      sync mailbox <@ actor.SetJobsCount @> count

    member this.Start(data: StartData) : Task<Result> = sync mailbox <@ actor.Start @> data

    member this.RequestContinue(data: RequestContinueData) : Task<Result> =
      sync mailbox <@ actor.RequestContinue @> data

    member this.State() : Task<State option> = sync mailbox <@ actor.State @> ()

    member this.ReportJobState(data: JobStateData) : Task<Result> = sync mailbox <@ reportJobState @> data

    member this.Config() : Task<Config> = sync mailbox <@ actor.Config @> ()
