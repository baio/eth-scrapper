namespace ScrapperTestContext

open ScrapperModels
open ScrapperModels.JobManager
open System.Threading.Tasks
open System.Threading
open Microsoft.Extensions.Logging

type ReportJobStateChanged = ScrapperModels.JobManager.State * ScrapperModels.JobManager.State -> unit

type Method = ReportJobState of JobStateData

type Message = Method * AsyncReplyChannel<ScrapperModels.JobManager.Result>

type JobManagerActor(env, fn: ReportJobStateChanged option) as this =
  let actor =
    JobManager.JobManagerBaseActor.JobManagerBaseActor(env) :> IJobManagerActor

  let lock' fn =
    task { return lock this (fun () -> fn ()) }

  let lockt (fn: 'a -> Task<_>) (x: 'a) =
    task { return lock this (fun () -> (fn x).Result) }

  let mailbox =
    MailboxProcessor<Message>.Start
      (fun inbox ->

        // the message processing function
        let rec loop () =
          async {

            // read a message
            let! (msg, replyChannel) = inbox.Receive()

            // process a message
            match msg with
            | ReportJobState data ->
              let! result =
                task {
                  let! pervState = actor.State()
                  let! result = actor.ReportJobState data
                  let! currState = actor.State()

                  match pervState, currState, fn with
                  | Some pervState, Some currState, Some fn -> fn (pervState, currState)
                  | _ -> ()

                  return result
                }
                |> Async.AwaitTask

              replyChannel.Reply result

            // loop to top
            return! loop ()
          }

        // start the loop
        loop ())

  interface IJobManagerActor with

    member this.Pause() : Task<Result> = lockt actor.Pause ()

    member this.Reset() : Task<Result> = lockt actor.Reset ()

    member this.Resume() : Task<Result> = lockt actor.Resume ()

    member this.SetJobsCount(count: uint) : Task<Result<Config, string>> = lockt actor.SetJobsCount count

    member this.Start(data: StartData) : Task<Result> = lockt actor.Start data

    member this.RequestContinue(data: RequestContinueData) : Task<Result> = lockt actor.RequestContinue data

    member this.State() : Task<State option> = actor.State()

    member this.ReportJobState(data: JobStateData) : Task<Result> = 
      task { return! mailbox.PostAndAsyncReply(fun rc -> (Method.ReportJobState data), rc) }

    member this.Config() : Task<Config> = lockt actor.Config ()
