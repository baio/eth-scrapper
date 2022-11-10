namespace ScrapperTestContext

open ScrapperModels
open ScrapperModels.JobManager
open System.Threading.Tasks
open System.Threading
open Microsoft.Extensions.Logging
open Microsoft.FSharp.Quotations
open FSharp.Linq.RuntimeHelpers

type ReportJobStateChanged = ScrapperModels.JobManager.State * ScrapperModels.JobManager.State -> unit

type Method = ReportJobState of JobStateData

type Call<'a, 'r> = Expr<'a -> Task<'r>> * 'a

type CallMessage<'r> = Expr<Task<'r>> * AsyncReplyChannel<'r>

type Message = Method * AsyncReplyChannel<ScrapperModels.JobManager.Result>

[<AutoOpen>]
module Mailbox =

  let private mailbox =
    MailboxProcessor<CallMessage<obj>>.Start
      (fun inbox ->

        // the message processing function
        let rec loop () =
          async {

            // read a message
            let! (msg, replyChannel) = inbox.Receive()

            let call = msg

            let result =
              <@ (%call) @>
              |> LeafExpressionConverter.EvaluateQuotation

            let result' = result :?> Task<obj>

            let! result'' = result' |> Async.AwaitTask

            replyChannel.Reply result''

            return! loop ()
          }

        loop ())

  let sync (expr: Expr<'a -> Task<'r>>) (data: 'a) =
    let expr' =
      <@ task {
        let! result = (%expr) data
        let result = result :> obj
        return result
      } @>

    task {
      let! result = mailbox.PostAndAsyncReply(fun rc -> expr', rc)
      return result :?> 'r
    }


type JobManagerActor(env, fn: ReportJobStateChanged option) as this =
  let actor =
    JobManager.JobManagerBaseActor.JobManagerBaseActor(env) :> IJobManagerActor

  let lock' fn =
    task { return lock this (fun () -> fn ()) }

  let lockt (fn: 'a -> Task<_>) (x: 'a) =
    task { return lock this (fun () -> (fn x).Result) }


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

    member this.Pause() : Task<Result> = sync <@ actor.Pause @> ()

    member this.Reset() : Task<Result> = sync <@ actor.Reset @> ()

    member this.Resume() : Task<Result> = sync <@ actor.Resume @> ()

    member this.SetJobsCount(count: uint) : Task<Result<Config, string>> = sync <@ actor.SetJobsCount @> count

    member this.Start(data: StartData) : Task<Result> = sync <@ actor.Start @> data

    member this.RequestContinue(data: RequestContinueData) : Task<Result> = sync <@ actor.RequestContinue @> data

    member this.State() : Task<State option> = sync <@ actor.State @> ()

    member this.ReportJobState(data: JobStateData) : Task<Result> = sync <@ reportJobState @> data

    member this.Config() : Task<Config> = sync <@ actor.Config @> ()
