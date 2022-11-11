namespace ScrapperTestContext


[<AutoOpen>]
module Mailbox =

  open System.Threading.Tasks
  open Microsoft.FSharp.Quotations
  open FSharp.Linq.RuntimeHelpers

  type CallMessage<'r> = (string * obj * Expr<Task<'r>>) * AsyncReplyChannel<'r>

  type OnBefore = string * string -> obj -> Task<obj>
  type OnAfter = string * string -> obj * obj * obj -> Task

  type MailboxHooks = OnBefore option * OnAfter option

  let createMailbox' (className: string) ((onBefore, onAfter): MailboxHooks) =
    MailboxProcessor<CallMessage<obj>>.Start
      (fun inbox ->

        let rec loop () =
          async {

            let! ((funName, arg, call), replyChannel) = inbox.Receive()

            let! result =
              task {
                let! beforeResult =
                  (match onBefore with
                   | Some onBefore -> onBefore (className, funName) arg
                   | None -> () |> box |> Task.FromResult)

                let result =
                  <@ (%call) @>
                  |> LeafExpressionConverter.EvaluateQuotation

                let! result = result :?> Task<obj>

                do!
                  (match onAfter with
                   | Some onAfter -> onAfter (className, funName) (result, arg, beforeResult)
                   | None -> () |> box |> Task.FromResult :> Task)

                return result
              }
              |> Async.AwaitTask

            replyChannel.Reply result


            return! loop ()
          }

        loop ())

  let createMailbox () = createMailbox' "unknown" (None, None)


  let rec getClassName =
    function
    | Patterns.FieldGet (_, methodInfo) -> methodInfo.DeclaringType.Name
    | Patterns.Let (_, expr, _) -> getClassName expr
    | _ as x -> failwith "Unexpected input"

  let rec getFunName =
    function
    | Patterns.Call (_, methodInfo, _) -> methodInfo.Name
    | Patterns.Lambda (_, expr) -> getFunName expr
    | Patterns.Let (_, _, expr) -> getFunName expr
    | _ as x -> failwith "Unexpected input"

  let sync (mailbox: MailboxProcessor<CallMessage<obj>>) (expr: Expr<'a -> Task<'r>>) (data: 'a) =

    let expr' =
      <@ task {
        let! result = (%expr) data
        let result = result :> obj
        return result
      } @>

    // After reading class name from expression, call `(%expr) data` stops to work
    //let className = getClassName expr
    let funName = getFunName expr

    task {
      let! result = mailbox.PostAndAsyncReply(fun rc -> ((funName, (box data), expr'), rc))
      return result :?> 'r
    }
