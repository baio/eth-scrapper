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

  type BlockRangeDTO =
    { From: System.Nullable<uint>
      To: System.Nullable<uint> }

  type ScrapperRequestDTO =
    { EthProviderUrl: string
      ContractAddress: string
      Abi: string
      BlockRange: BlockRangeDTO }

  let private toDTO (request: ScrapperRequest) : ScrapperRequestDTO =
    { EthProviderUrl = request.EthProviderUrl
      ContractAddress = request.ContractAddress
      Abi = request.Abi
      BlockRange =
        { From =
            match request.BlockRange.From with
            | Some from -> System.Nullable(from)
            | None -> System.Nullable()
          To =
            match request.BlockRange.To with
            | Some _to -> System.Nullable(_to)
            | None -> System.Nullable() } }

  let private checkStop (result: ScrapperResult) =
    match result with
    // read successfully till the latest block
    | Ok success when success.RequestBlockRange.To = None -> true
    | Error error ->
      match error.Data with
      // read successfully till the latest block
      | EmptyResult when error.RequestBlockRange.To = None -> true
      | _ -> false
    | _ -> false


  let private runScrapper (proxyFactory: Client.IActorProxyFactory) actorId scrapperRequest =
    let dto = scrapperRequest |> toDTO
    invokeActor<ScrapperRequestDTO, bool> proxyFactory actorId "ScrapperActor" "scrap" dto


  let private STATE_NAME = "state"
  let private SCHEDULE_TIMER_NAME = "timer"

  [<Actor(TypeName = "scrapper-dispatcher")>]
  type ScrapperDispatcherActor(host: ActorHost) as this =
    inherit Actor(host)
    let logger = ActorLogging.create host
    let stateManager = stateManager<State> STATE_NAME this.StateManager

    let runScrapper (state: State option) (scrapperRequest: ScrapperRequest) =

      logger.LogDebug("Run scrapper with {@data} {@state}", scrapperRequest, state)

      task {
        let! result = runScrapper this.ProxyFactory this.Id scrapperRequest

        logger.LogDebug("Run scrapper result {@result}", result)

        let finishDate =
          match state with
          | Some state -> state.FinishDate
          | None -> None

        match result with
        | Ok _ ->
          let state: State =
            { Status = Status.Continue
              Request = scrapperRequest
              Date = epoch ()
              FinishDate = finishDate }

          do! stateManager.Set state

          return state |> Ok
        | Error _ ->
          let state: State =
            { Status =
                { Data =
                    { AppId = AppId.Dispatcher
                      Status = AppId.Scrapper |> CallChildActorFailure }
                  RetriesCount = 0u }
                |> Status.Failure
              Request = scrapperRequest
              Date = epoch ()
              FinishDate = finishDate }


          do! stateManager.Set state

          return state |> ActorFailure |> Error
      }

    interface IScrapperDispatcherActor with

      member this.Start data =

        logger.LogDebug("Start with {@data}", data)

        task {

          let! state = stateManager.Get()

          match state with
          | Some state ->
            let error = "Trying to start version which already started"
            logger.LogError(error, data)
            return (state, error) |> StateConflict |> Error
          | None ->
            let scrapperRequest: ScrapperRequest =
              { EthProviderUrl = data.EthProviderUrl
                ContractAddress = data.ContractAddress
                Abi = data.Abi
                BlockRange = { From = None; To = None } }

            return! runScrapper None scrapperRequest
        }

      member this.Continue data =
        logger.LogDebug("Continue with {@data}", data)
        let me = this :> IScrapperDispatcherActor

        task {
          let! state = stateManager.Get()

          match state with
          | Some state when state.Status = Status.Pause ->
            let error = "Actor in paused state, skip continue"
            logger.LogDebug(error)
            return (state, error) |> StateConflict |> Error
          | _ ->
            let blockRange = nextBlockRangeCalc data.Result

            let scrapperRequest: ScrapperRequest =
              { EthProviderUrl = data.EthProviderUrl
                ContractAddress = data.ContractAddress
                Abi = data.Abi
                BlockRange = blockRange }

            match checkStop data.Result with
            | false ->

              logger.LogDebug("Stop check is false, continue", scrapperRequest)


              match state with
              | None ->
                logger.LogWarning("Continue, but state not found")
                ()
              | _ -> ()

              return! runScrapper state scrapperRequest

            | true ->

              logger.LogInformation("Stop check is true, finish")

              let state: State =
                { Status = Status.Finish
                  Request = scrapperRequest
                  Date = epoch ()
                  FinishDate = epoch () |> Some }

              do! stateManager.Set state

              let! result = me.Schedule()

              return result
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

              return! runScrapper (Some state) updatedState.Request
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
