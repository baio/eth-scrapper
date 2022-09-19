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


  let private getEthBlocksCount (ethProviderUrl: string) =
    task {
      let web3 = new Web3(ethProviderUrl)
      let! result = web3.Eth.Blocks.GetBlockNumber.SendRequestAsync()
      return result.Value |> uint
    }

  [<RequireQualifiedAccess>]
  type private CheckStop =
    | Continue
    | Stop
    | ContinueToLatest of BlockRange * TargetBlockRange

  let private checkStop ethProviderUrl (target: TargetBlockRange) (result: ScrapperResult) =
    let checkStopBlockRange (blockRange: BlockRange) =
      match (target.ToLatest, blockRange.To >= target.Range.To) with
      // update `to` block when achive target reange end (`to`)
      | (true, true) ->
        // achive latest target block, check if latest block of eth changed
        task {
          let! latestBlock = getEthBlocksCount ethProviderUrl

          if latestBlock > target.Range.To then
            let range: BlockRange =
              { From = blockRange.To
                To = latestBlock }

            let target =
              { ToLatest = true
                Range =
                  { From = target.Range.From
                    To = latestBlock } }

            return CheckStop.ContinueToLatest(range, target)
          else
            return CheckStop.Stop
        }
      | (false, true) -> CheckStop.Stop |> Task.FromResult
      | (_, false) -> CheckStop.Continue |> Task.FromResult

    match result with
    // read successfully till the latest block
    | Ok success -> checkStopBlockRange success.BlockRange
    | Error error ->
      match error.Data with
      // read successfully till the latest block
      | EmptyResult -> checkStopBlockRange error.BlockRange
      | _ -> CheckStop.Continue |> Task.FromResult


  let private runScrapper (proxyFactory: Client.IActorProxyFactory) actorId scrapperRequest =
    let dto = scrapperRequest |> toDTO
    invokeActor<ScrapperRequestDTO, bool> proxyFactory actorId "ScrapperActor" "scrap" dto


  let private STATE_NAME = "state"
  let private SCHEDULE_TIMER_NAME = "timer"
  let private LATEST_SUCCESSFULL_BLOCK_RANGES_SIZE = 5

  let updateLatesSuccessfullBlockRanges (state: State) (result: Success) =
    let requestRange = result.BlockRange.To - result.BlockRange.From
    let itemsLength = result.ItemsCount

    let itemsPerBlock =
      (System.Convert.ToSingle itemsLength)
      / (System.Convert.ToSingle requestRange)

    let ranges =
      itemsPerBlock :: state.ItemsPerBlock
      |> List.truncate LATEST_SUCCESSFULL_BLOCK_RANGES_SIZE

    { state with ItemsPerBlock = ranges }


  type RunScrapperState =
    | Start of ethProviderUrl: string
    | Continue of State

  let private createScrapperRequest (data: ContinueData) (blockRange: RequestBlockRange) : ScrapperRequest =
    { EthProviderUrl = data.EthProviderUrl
      ContractAddress = data.ContractAddress
      Abi = data.Abi
      BlockRange = blockRange }

  [<Actor(TypeName = "scrapper-dispatcher")>]
  type ScrapperDispatcherActor(host: ActorHost) as this =
    inherit Actor(host)
    let logger = ActorLogging.create host
    let stateManager = stateManager<State> STATE_NAME this.StateManager

    let runScrapper (state: RunScrapperState) (scrapperRequest: ScrapperRequest) =

      logger.LogDebug("Run scrapper with {@data} {@state}", scrapperRequest, state)

      task {
        let! result = runScrapper this.ProxyFactory this.Id scrapperRequest

        logger.LogDebug("Run scrapper result {@result}", result)

        let finishDate =
          match state with
          | Continue state -> state.FinishDate
          | Start _ -> None

        let latestSuccesses =
          match state with
          | Continue state -> state.ItemsPerBlock
          | Start _ -> []

        let! target =
          match state with
          | Continue state -> state.Target |> Task.FromResult
          | Start ethProviderUrl ->
            task {
              let! to' = getEthBlocksCount ethProviderUrl

              return
                { ToLatest = true
                  Range = { From = 0u; To = to' } }
            }

        match result with
        | Ok _ ->
          let state: State =
            { Status = Status.Continue
              Request = scrapperRequest
              Date = epoch ()
              FinishDate = finishDate
              ItemsPerBlock = latestSuccesses
              Target = target }

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
              FinishDate = finishDate
              ItemsPerBlock = latestSuccesses
              Target = target }

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

            return! runScrapper (Start data.EthProviderUrl) scrapperRequest
        }

      member this.Continue data =
        logger.LogDebug("Continue with {@data}", data)
        let me = this :> IScrapperDispatcherActor

        task {
          let! state = stateManager.Get()

          match state with
          | Some state ->
            match state.Status with
            | Status.Pause
            | Status.Failure _ ->
              let error = $"Actor in {state.Status} state, skip continue"
              logger.LogDebug(error)
              return (state, error) |> StateConflict |> Error
            | _ ->

              logger.LogDebug("Actor in {@state} with {@data}, calc next request", state, data.Result)

              let state =
                match data.Result with
                | Ok success -> updateLatesSuccessfullBlockRanges state success
                | Error _ -> state

              logger.LogDebug("{@state} with updated block ranges", state)

              let! checkStopResult = checkStop data.EthProviderUrl state.Target data.Result

              logger.LogDebug("Check stop {@result}", checkStopResult)

              match checkStopResult with
              | CheckStop.Continue ->

                let blockRange = NextBlockRangeCalc2.calc state.ItemsPerBlock data.Result

                let scrapperRequest = createScrapperRequest data blockRange

                logger.LogDebug("Next scrapper request {@request}", scrapperRequest)

                logger.LogDebug(
                  "Stop check is CheckStop.Continue, continue with {@request} {@state}",
                  scrapperRequest,
                  state
                )

                return! runScrapper (Continue state) scrapperRequest
              | CheckStop.ContinueToLatest (range, target) ->

                let scrapperRequest =
                  createScrapperRequest
                    data
                    { From = (Some range.From)
                      To = (Some range.To) }

                let state = { state with Target = target }

                logger.LogDebug(
                  "Stop check is CheckStop.ContinueToLatest, continue with {@request} {@state} ",
                  scrapperRequest,
                  state
                )

                return! runScrapper (Continue state) scrapperRequest

              | CheckStop.Stop ->

                logger.LogInformation("Stop check is CheckStop.Stop, finish")

                let state: State =
                  { state with
                      Status = Status.Finish
                      Date = epoch ()
                      FinishDate = epoch () |> Some }

                do! stateManager.Set state

                logger.LogDebug("New {@state} set", state)

                let! result = me.Schedule()

                return result
          | None ->
            logger.LogError("State not found")
            return StateNotFound |> Error
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
