namespace ScrapperAPI.Controllers

module private JobsManagerDTO =
  open ScrapperModels.JobManager
  open Microsoft.AspNetCore.Mvc

  let mapState (state: State) =
    {| jobsCount = state.AvailableJobsCount
       status =
        match state.Status with
        | Status.Continue -> "continue"
        | Status.Initial -> "initial"
        | Status.Success -> "success"
        | Status.Failure -> "failure"
        | Status.PartialFailure _ -> "partial-failure" |}

  let mapError (err: Error) =
    match err with
    | StateConflict (state, error) ->
      ConflictObjectResult(
        {| State = (mapState state)
           Error = error |}
      )
      :> IActionResult
    | StateNotFound -> NotFoundObjectResult() :> IActionResult
    | ActorFailure state -> UnprocessableEntityObjectResult({| State = mapState (state) |}) :> IActionResult
    | ValidationFailure msg -> BadRequestObjectResult({| Message = msg |}) :> IActionResult

  let mapSuccess (state: State) =
    state |> mapState |> OkObjectResult :> IActionResult

  let mapResult (result: Result) =
    match result with
    | Ok state -> mapSuccess state
    | Error err -> mapError err
