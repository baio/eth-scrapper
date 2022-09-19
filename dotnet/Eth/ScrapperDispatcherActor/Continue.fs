namespace ScrapperDispatcherActor

[<AutoOpen>]
module internal Continue =

  open Dapr.Actors
  open Dapr.Actors.Runtime
  open System.Threading.Tasks
  open ScrapperModels
  open Microsoft.Extensions.Logging
  open Common.DaprActor
  open Common.DaprActor.ActorResult
  open System
  open Nethereum.Web3

  let private LATEST_SUCCESSFULL_BLOCK_RANGES_SIZE = 5

  let private updateLatesSuccessfullBlockRanges (state: State) (result: Success) =
    let requestRange = result.BlockRange.To - result.BlockRange.From
    let itemsLength = result.ItemsCount

    let itemsPerBlock =
      (System.Convert.ToSingle itemsLength)
      / (System.Convert.ToSingle requestRange)

    let ranges =
      itemsPerBlock :: state.ItemsPerBlock
      |> List.truncate LATEST_SUCCESSFULL_BLOCK_RANGES_SIZE

    { state with ItemsPerBlock = ranges }

  let private createScrapperRequest (data: ContinueData) (blockRange: BlockRange) : ScrapperRequest =
    { EthProviderUrl = data.EthProviderUrl
      ContractAddress = data.ContractAddress
      Abi = data.Abi
      BlockRange = blockRange }

  let continue ((runScrapperEnv, env): RunScrapperEnv * ActorEnv) (data: ContinueData) =

    let logger = env.Logger
    let runScrapper = runScrapper runScrapperEnv

    logger.LogDebug("Continue with {@data}", data)

    task {
      let! state = env.GetState()

      match state with
      | Some state ->
        match state.Status with
        | Status.Pause ->
          let error = $"Actor in {state.Status} state, skip continue"
          logger.LogDebug(error)
          return (state, error) |> StateConflict |> Error
        | Status.Failure _
        | Status.Continue
        | Status.Schedule
        | Status.Finish ->

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

            let scrapperRequest = createScrapperRequest data range

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

            do! env.SetState state

            logger.LogDebug("New {@state} set", state)

            return state |> Ok
      | None ->
        logger.LogError("State not found")
        return StateNotFound |> Error
    }
