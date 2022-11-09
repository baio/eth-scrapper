namespace ScrapperTestContext

open ScrapperModels
open ScrapperModels.ScrapperDispatcher
open System.Threading.Tasks
open System.Threading
open Microsoft.Extensions.Logging

type JobActor(env) as this =
  let actor =
    ScrapperDispatcherActor.ScrapperDispatcherBaseActor.ScrapperDispatcherBaseActor(env) :> IScrapperDispatcherActor


  let lockt (fn: 'a -> Task<_>) (x: 'a) =
    task { return lock this (fun () -> (fn x).Result) }

  interface IScrapperDispatcherActor with

    member this.Start data = lockt actor.Start data

    member this.Continue data = lockt actor.Continue data

    member this.Pause() = lockt actor.Pause ()

    member this.Resume() = lockt actor.Resume ()

    member this.State() = lockt actor.State ()

    member this.Reset() = lockt actor.Reset ()

    member this.Failure data = lockt actor.Failure data

    member this.ConfirmContinue data = lockt actor.ConfirmContinue data
