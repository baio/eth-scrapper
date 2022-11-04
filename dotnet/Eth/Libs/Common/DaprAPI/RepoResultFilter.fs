namespace Common.DaprAPI

[<AutoOpen>]
module RepoResultFilter =

  open Common.Repo
  open Microsoft.AspNetCore.Mvc
  open Microsoft.FSharp.Reflection
  open Microsoft.AspNetCore.Mvc.Filters

  type ResultMapper = IActionResult -> IActionResult option

  let mapRepoError =
    function
    | NotFound -> () |> NotFoundObjectResult :> IActionResult
    | Frobidden -> () |> ForbidResult :> IActionResult
    | Conflict err -> err |> ConflictObjectResult :> IActionResult
    | Unexpected _ -> 500 |> StatusCodeResult :> IActionResult

  let private mapActionResult (actionResult: IActionResult) =
    match actionResult with
    | :? ObjectResult as objectResult ->
      let objectResultValue = objectResult.Value

      if
        objectResultValue <> null
        && FSharpType.IsUnion(objectResultValue.GetType())
      then
        try
          match FSharpValue.GetUnionFields(objectResultValue, objectResultValue.GetType()) with
          | x, vals when x.Name = "Error" ->
            match vals[0] with
            | :? RepoError as err -> err |> mapRepoError
            | _ -> 500 |> StatusCodeResult :> IActionResult
          | x, vals when x.Name = "Ok" -> vals[0] |> OkObjectResult :> IActionResult
          | _, vals -> vals |> OkObjectResult :> IActionResult
        with
        | _ as err ->
          printfn "%O" actionResult
          printfn "========================="
          printfn "%O" err
          actionResult
      else
        actionResult
    | _ as actionResult -> actionResult


  type RepoResultFilter(mapper: ResultMapper option) =
    interface IAsyncResultFilter with
      member this.OnResultExecutionAsync(ctx, next) =
        task {

          let result =
            match mapper with
            | Some mapper -> mapper ctx.Result
            | None -> None

          match result with
          | Some result -> ctx.Result <- result
          | None -> ctx.Result <- mapActionResult ctx.Result

          return! next.Invoke()
        }
