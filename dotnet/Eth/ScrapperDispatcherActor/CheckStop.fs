namespace ScrapperDispatcherActor

[<AutoOpen>]
module internal CheckStop =

  open ScrapperModels
  open ScrapperModels.ScrapperDispatcherActor
  open System.Threading.Tasks

  [<RequireQualifiedAccess>]
  type CheckStop =
    | Continue
    | Stop
    | ContinueToLatest of BlockRange * TargetBlockRange

  let checkStop ethProviderUrl (target: TargetBlockRange) (result: ScrapperResult) =
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
