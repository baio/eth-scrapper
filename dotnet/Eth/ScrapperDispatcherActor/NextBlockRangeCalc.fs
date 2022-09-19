namespace ScrapperDispatcherActor

[<RequireQualifiedAccess>]
module NextBlockRangeCalc =
  open ScrapperModels

  // TODO !!!
  let private successNextBlockRangeCalc (result: Success) =

    // Take from current "To" block till "latest"
    let resultRange = { From = result.BlockRange.To; To = 0u }
    resultRange

  let private errorNextBlockRangeCalc (error: Error) =
    match error.Data with
    | LimitExceeded ->
      // Decrease range by half
      { From = error.BlockRange.From
        To =
          error.BlockRange.From
          + (error.BlockRange.To - error.BlockRange.From) / 2u }
    | EmptyResult ->
      // Start from latest To now
      { From = error.BlockRange.To; To = 0u }
    | Unknown ->
      // Left as it is
      { From = error.BlockRange.From
        To = error.BlockRange.To }

  let nextBlockRangeCalc =
    function
    | Ok success -> successNextBlockRangeCalc success
    | Error error -> errorNextBlockRangeCalc error
