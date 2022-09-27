namespace ScrapperDispatcherActor

[<AutoOpen>]
module ScrapperDispatcherActor =

  open Dapr.Actors
  open Dapr.Actors.Runtime
  open ScrapperModels.ScrapperDispatcher
  open ScrapperModels
  open Microsoft.Extensions.Logging
  open Common.DaprActor
  open System

  let private invokeScrapper (proxyFactory: Client.IActorProxyFactory) actorId scrapperRequest =
    invokeActor<ScrapperRequest, bool> proxyFactory actorId "ScrapperActor" "scrap" scrapperRequest
      
  let private invokeRequestContinue (proxyFactory: Client.IActorProxyFactory) actorId request =
    let actor = proxyFactory.CreateActorProxy<JobManager.IJobManagerActor>(actorId, "job-manager")
    actor.RequestContinue request

  let private STATE_NAME = "state"
  let private SCHEDULE_TIMER_NAME = "timer"

  [<Actor(TypeName = "scrapper-dispatcher")>]
  type ScrapperDispatcherActor(host: ActorHost) as this =
    inherit Actor(host)
    let logger = ActorLogging.create host
    let stateManager = stateManager<State> STATE_NAME this.StateManager

    let runScrapperEnv: RunScrapperEnv =
      { InvokeActor = (invokeScrapper host.ProxyFactory host.Id)
        SetState = stateManager.Set
        Logger = logger }

    let requestContinueEnv: RequestContinueEnv =
      { InvokeActor = (invokeRequestContinue host.ProxyFactory host.Id)
        SetState = stateManager.Set
        Logger = logger }


    let actorEnv =
      { GetState = stateManager.Get
        SetState = stateManager.Set
        Logger = logger }

    interface IScrapperDispatcherActor with

      member this.Start data = start (runScrapperEnv, actorEnv) data

      member this.Continue data =
        logger.LogDebug("Continue with {@data}", data)
        let me = this :> IScrapperDispatcherActor

        task {
          let! result = continue (requestContinueEnv, actorEnv) host.Id data

          match result with
          | Ok result when result.Status = Status.Finish ->
            let! result = me.Schedule()

            return result
          | _ -> return result
        }


      member this.Pause() = pause actorEnv

      member this.Resume() = resume (runScrapperEnv, actorEnv)

      member this.State() = stateManager.Get()

      member this.Reset() = stateManager.Remove()

      member this.Schedule() =
        let me = this :> Actor

        let scheduleHandler dueTime =
          task {
            let! _ =
              me.RegisterTimerAsync(SCHEDULE_TIMER_NAME, "Resume", [||], TimeSpan.FromSeconds(dueTime), TimeSpan.Zero)

            return ()
          }

        schedule (actorEnv, scheduleHandler)

      member this.Failure(data: FailureData) = failure (runScrapperEnv, actorEnv) data

      member this.ConfirmContinue(data: ConfirmContinueData) =
        confirmContinue (runScrapperEnv, actorEnv) data
