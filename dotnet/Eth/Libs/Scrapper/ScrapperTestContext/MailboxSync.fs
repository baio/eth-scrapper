namespace ScrapperTestContext


[<AutoOpen>]
module Mailbox =

  open System.Threading.Tasks
  open Microsoft.FSharp.Quotations
  open FSharp.Linq.RuntimeHelpers

  type Call<'a, 'r> = Expr<'a -> Task<'r>> * 'a

  type CallMessage<'r> = Expr<Task<'r>> * AsyncReplyChannel<'r>

  let createMailbox () =
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

  let sync (mailbox: MailboxProcessor<CallMessage<obj>>) (expr: Expr<'a -> Task<'r>>) (data: 'a) =
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
