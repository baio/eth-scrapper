namespace ScrapperTestContext

open ScrapperModels
open ScrapperModels.ScrapperDispatcher
open System.Threading.Tasks
open System.Threading
open Microsoft.Extensions.Logging

type JobActor(env) =

  let mailbox = createMailbox ()

  let actor =
    ScrapperDispatcherActor.ScrapperDispatcherBaseActor.ScrapperDispatcherBaseActor(env) :> IScrapperDispatcherActor

  interface IScrapperDispatcherActor with

    member this.Start data = sync mailbox <@ actor.Start @> data

    member this.Continue data = sync mailbox <@ actor.Continue @> data

    member this.Pause() = sync mailbox <@ actor.Pause @> ()

    member this.Resume() = sync mailbox <@ actor.Resume @> ()

    member this.State() = sync mailbox <@ actor.State @> ()

    member this.Reset() = sync mailbox <@ actor.Reset @> ()

    member this.Failure data = sync mailbox <@ actor.Failure @> data

    member this.ConfirmContinue data =
      sync mailbox <@ actor.ConfirmContinue @> data
