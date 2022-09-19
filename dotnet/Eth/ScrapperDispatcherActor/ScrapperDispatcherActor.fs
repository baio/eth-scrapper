namespace ScrapperDispatcherActor

[<AutoOpen>]
module ScrapperDispatcherActor =

  open Dapr.Actors
  open Dapr.Actors.Runtime
  open System.Threading.Tasks
  open ScrapperModels
  open Microsoft.Extensions.Logging
  open Common.DaprActor
  open Common.DaprActor.ActorResult
  open System
  open Nethereum.Web3

  let private invokeActor (proxyFactory: Client.IActorProxyFactory) actorId scrapperRequest =
    invokeActor<ScrapperRequest, bool> proxyFactory actorId "ScrapperActor" "scrap" scrapperRequest

  let private STATE_NAME = "state"
  let private SCHEDULE_TIMER_NAME = "timer"

  [<Actor(TypeName = "scrapper-dispatcher")>]
  type ScrapperDispatcherActor(host: ActorHost) as this =
    inherit Actor(host)
    let logger = ActorLogging.create host
    let stateManager = stateManager<State> STATE_NAME this.StateManager

    let runScrapperEnv =
      { InvokeActor = (invokeActor host.ProxyFactory)
        SetState = stateManager.Set
        Logger = logger }

    let actorEnv = (runScrapperEnv, host.Id)

    let runScrapper = runScrapper runScrapperEnv host.Id

    interface IScrapperDispatcherActor with

      member this.Start data =

        task {

          let! state = stateManager.Get()

          return! start actorEnv state data
        }

      member this.Continue data =
        logger.LogDebug("Continue with {@data}", data)
        let me = this :> IScrapperDispatcherActor

        task {
          let! state = stateManager.Get()

          let! result = continue actorEnv state data

          match result with
          | Ok result when result.Status = Status.Finish ->
            let! result = me.Schedule()

            return result
          | _ -> return result
        }


      member this.Pause() =
        task {
          let! state = stateManager.Get()

          match state with
          | Some state ->
            if state.Status = Status.Continue
               || state.Status = Status.Schedule then
              let state =
                { state with
                    Status = Status.Pause
                    Date = epoch () }

              do! stateManager.Set state

              return state |> Ok
            else
              let error = "Actor in a wrong state"
              logger.LogDebug(error)
              return (state, error) |> StateConflict |> Error
          | None -> return StateNotFound |> Error

        }

      member this.Resume() =
        task {
          let! state = stateManager.Get()

          match state with
          | Some state ->
            match state.Status with
            | Status.Pause
            | Status.Finish
            | Status.Failure _ ->

              let updatedState =
                { state with
                    Status = Status.Continue
                    Date = epoch () }

              logger.LogInformation("Resume with {@pervState} {@state}", state, updatedState)

              return! runScrapper (Continue state) updatedState.Request
            | _ ->
              let error = "Actor in a wrong state"
              logger.LogDebug(error)
              return (state, error) |> StateConflict |> Error
          | _ ->
            logger.LogWarning("Resume state not found or Continue")
            return StateNotFound |> Error

        }

      member this.State() = stateManager.Get()

      member this.Reset() = stateManager.Remove()

      member this.Schedule() =
        let dueTime = 60.
        logger.LogInformation("Try schedule {dueTime}", dueTime)

        task {
          let! state = stateManager.Get()

          match state with
          | Some state ->
            if state.Status = Status.Finish then
              logger.LogInformation("Run schedule with {@state}", state)

              let updatedState =
                { state with
                    Status = Status.Schedule
                    Date = epoch () }

              do! stateManager.Set updatedState

              let! _ =
                this.RegisterTimerAsync(
                  SCHEDULE_TIMER_NAME,
                  "Resume",
                  [||],
                  TimeSpan.FromSeconds(dueTime),
                  TimeSpan.Zero
                )

              return state |> Ok
            else
              let error = "Can't schedule, wrong state {@state}"
              logger.LogDebug("Can't schedule, wrong state {@state}", state)
              return (state, error) |> StateConflict |> Error
          | None ->
            logger.LogDebug("Can't schedule, wrong state {@state}", state)
            return StateNotFound |> Error
        }

      member this.Failure(data: FailureData) =
        task {
          let! state = stateManager.Get()

          match state with
          | Some state ->
            let state =
              { state with
                  Status =
                    { Data = data; RetriesCount = 0u }
                    |> Status.Failure
                  Date = epoch () }

            logger.LogInformation("Failure with {@state}", state)

            do! stateManager.Set(state)

            return Some state

          | None ->
            logger.LogWarning("Failure {@failure} but state is not found", state)

            return None
        }
