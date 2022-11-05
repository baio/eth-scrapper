namespace ScrapperAPI

module internal ResultMapper =
  open ScrapperAPI.Services.JobManagerService
  open ScrapperModels.JobManager
  open Microsoft.AspNetCore.Mvc
  open Common.DaprAPI
  open Microsoft.FSharp.Reflection
  //open ScrapperAPI.Services.JobManagerService


  let private mapError (err: Error) =
    match err with
    | StateConflict (state, error) -> ConflictObjectResult({| State = state; Error = error |}) :> IActionResult
    | StateNotFound -> NotFoundObjectResult() :> IActionResult
    | ActorFailure state -> UnprocessableEntityObjectResult({| State = state |}) :> IActionResult
    | ValidationFailure msg -> BadRequestObjectResult({| Message = msg |}) :> IActionResult

  let private mapExecutionError (err: ExecutionError) =
    match err with
    | ExecutionError.ActorFailure error -> mapError error
    | ExecutionError.RepoError error -> mapRepoError error

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
            | :? ExecutionError as err -> err |> mapExecutionError |> Some
            | :? Error as err -> err |> mapError |> Some
            | _ -> None
          | x, vals when x.Name = "Ok" -> None
          | _, vals -> None
        else
          None
      | _ -> None
