namespace ScrapperAPI

open Microsoft.FSharp.Reflection

module internal ResultMapper =
  open ScrapperModels.JobManager
  open Microsoft.AspNetCore.Mvc
  open Common.DaprAPI

  // let private mapState (state: State) =
  //   {| jobsCount = state.AvailableJobsCount
  //      date = state.LatestUpdateDate
  //      status =
  //       match state.Status with
  //       | Status.Continue -> "continue"
  //       | Status.Initial -> "initial"
  //       | Status.Success -> "success"
  //       | Status.Failure -> "failure"
  //       | Status.PartialFailure _ -> "partial-failure" |}

  let private mapError (err: Error) =
    match err with
    | StateConflict (state, error) ->
      ConflictObjectResult(
        {| State = state
           Error = error |}
      )
      :> IActionResult
    | StateNotFound -> NotFoundObjectResult() :> IActionResult
    | ActorFailure state -> UnprocessableEntityObjectResult({| State = state |}) :> IActionResult
    | ValidationFailure msg -> BadRequestObjectResult({| Message = msg |}) :> IActionResult

  // let private mapSuccess (state: State) =
  //   state |> mapState |> OkObjectResult :> IActionResult

  let mapResult: ResultMapper =
    fun actionResult ->
      match actionResult with
      | :? ObjectResult as objectResult ->
        let objectResultValue = objectResult.Value

        if
          objectResultValue <> null
          && FSharpType.IsUnion(objectResultValue.GetType())
        then
          match FSharpValue.GetUnionFields(objectResultValue, objectResultValue.GetType()) with
          | x, vals when x.Name = "Error" ->
            match vals[0] with
            | :? Error as err -> err |> mapError |> Some
            | _ -> None
          | x, vals when x.Name = "Ok" -> None
          | _, vals -> None
        else
          None
      | _ -> None
