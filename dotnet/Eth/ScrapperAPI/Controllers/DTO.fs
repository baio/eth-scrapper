namespace ScrapperAPI.Controllers

module private DTO =
  open ScrapperModels
  open Scrapper.Repo.PeojectsRepo
  open Microsoft.AspNetCore.Mvc


  let toNullable =
    function
    | Some x -> x |> box
    | None -> null

  let mapState (state: State) =
    {| request =
        {| state.Request with
             BlockRange =
               {| From = state.Request.BlockRange.From |> Option.toNullable
                  To = state.Request.BlockRange.To |> Option.toNullable |} |}
       date = state.Date
       finishDate = state.FinishDate
       status =
        match state.Status with
        | Status.Continue -> "continue"
        | Status.Pause -> "pause"
        | Status.Finish -> "finish"
        | Status.Schedule -> "schedule"
        | Status.Failure _ -> "failure" |}

  let mapProjectsWithViewStates (projects: ProjectWithVresionsAndState list) =
    projects
    |> List.map (fun project ->
      {| project with
           Versions =
             project.Versions
             |> List.map (fun version ->
               {| version with
                    State = version.State |> Option.map mapState |> toNullable |}) |})

  let mapScrapperDispatcherActorError (err: ScrapperDispatcherActorError) =
    match err with
    | StateConflict (state, error) ->
      ConflictObjectResult(
        {| State = (mapState state)
           Error = error |}
      )
      :> IActionResult
    | StateNotFound -> NotFoundObjectResult() :> IActionResult
    | ActorFailure state -> UnprocessableEntityObjectResult({| State = mapState (state) |}) :> IActionResult

  let mapScrapperDispatcherActorSuccess (state: State) =
    state |> mapState |> OkObjectResult :> IActionResult

  let mapScrapperDispatcherActorResult (result: ScrapperDispatcherActorResult) =
    match result with
    | Ok state -> mapScrapperDispatcherActorSuccess state
    | Error err -> mapScrapperDispatcherActorError err
