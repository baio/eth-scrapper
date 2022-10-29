namespace ScrapperAPI.Controllers

open Microsoft.AspNetCore.Mvc
open Microsoft.Extensions.Logging
open Microsoft.AspNetCore.Authorization
open Common.DaprState
open Scrapper.Repo.PeojectsRepo
open ScrapperAPI.Services.JobManagerService

[<ApiController>]
[<Route("projects")>]
type ProjectsController(env: StateEnv) =
  inherit ControllerBase()
  let repo = createRepo env

  [<HttpPost>]
  member this.Post(data: CreateProjectEntity) = repo.Create data


  [<HttpGet>]
  member this.GetAll() =

    env.App.Dapr.GetStateEntryAsync
    task {
      let! result = getProjectVersionsWithState (repo, )

      match result with
      | Ok result ->
        let result =
          result
          |> JobsManagerDTO.mapProjectsWithVersionStates

        return result |> Ok
      | Error err -> return err |> Error
    }

  [<HttpDelete("{projectId}")>]
  member this.Delete(projectId: string) = repo.Delete projectId
