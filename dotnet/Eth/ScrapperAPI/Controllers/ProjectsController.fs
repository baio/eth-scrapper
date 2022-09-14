namespace ScrapperAPI.Controllers

open Microsoft.AspNetCore.Mvc
open Microsoft.Extensions.Logging
open Microsoft.AspNetCore.Authorization
open Common.DaprState
open Scrapper.Repo.PeojectsRepo
open ScrapperAPI.Services.ScrapperDispatcherService

[<ApiController>]
[<Route("projects")>]
type ProjectsController(env: DaprStoreEnv) =
  inherit ControllerBase()
  let repo = createRepo env

  [<HttpPost>]
  member this.Post(data: CreateProjectEntity) = repo.Create data

  [<HttpGet>]
  member this.GetAll() =
    task {
      let! result = repo.GetAllWithVerions()

      match result with
      | Ok result ->
        let! result = result |> collectProjectVersionsWithState

        let result = result |> DTO.mapProjectsWithViewStates

        return result |> Ok
      | Error err -> return err |> Error
    }

  [<HttpDelete("{projectId}")>]
  member this.Delete(projectId: string) = repo.Delete projectId
