namespace ScrapperDispatcherActor

[<AutoOpen>]
module NextBlockRangeCalc =
  open ScrapperModels

  let private successNextBlockRangeCalc (result: Success) =

    // Take from current "To" block till "latest"
    let resultRange =
      { From = Some result.BlockRange.To
        To = None }
    resultRange

  let private errorNextBlockRangeCalc (error: Error) =
    match error.Data with
    | LimitExceeded ->
      // Decrease range by half
      { From = Some error.BlockRange.From
        To = Some(error.BlockRange.From + (error.BlockRange.To - error.BlockRange.From) / 2u) }
    | EmptyResult ->
      // Start from latest To now
      { From = Some error.BlockRange.To 
        To = None }
    | Unknown ->
      // Left as it is
      { From = Some error.BlockRange.From
        To = Some error.BlockRange.To }

  let nextBlockRangeCalc =
    function
    | Ok success -> successNextBlockRangeCalc success
    | Error error -> errorNextBlockRangeCalc error
