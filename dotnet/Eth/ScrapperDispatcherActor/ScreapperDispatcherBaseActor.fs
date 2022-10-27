namespace ScrapperDispatcherActor

[<AutoOpen>]
module ScrapperDispatcherBaseActor =

  open Dapr.Actors
  open Dapr.Actors.Runtime
  open ScrapperModels.ScrapperDispatcher
  open ScrapperModels
  open Microsoft.Extensions.Logging
  open Common.DaprActor
  open System

  type ScrapperDispatcherActor(env: Env) as this =

    interface IScrapperDispatcherActor with

      member this.Start data = start (runScrapperEnv, actorEnv) data

      member this.Continue data =
        logger.LogDebug("Continue with {@data}", data)
        let me = this :> IScrapperDispatcherActor

        task {
          let! result = continue (requestContinueEnv, actorEnv) host.Id data

          return result
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
