namespace ScrapperAPI.Controllers

open Microsoft.AspNetCore.Mvc
open Microsoft.Extensions.Logging
open Microsoft.AspNetCore.Authorization
open Common.DaprState
open Scrapper.Repo.PeojectsRepo
open ScrapperAPI.Services.JobManagerService
open Dapr.Abstracts
open Scrapper.Repo

[<ApiController>]
[<Route("projects")>]
type ProjectsController(repoEnv: RepoEnv, actorFactory: JobManagerActorFactory) =
  inherit ControllerBase()
  let repo = createRepo repoEnv

  [<HttpPost>]
  member this.Post(data: CreateProjectEntity) = createProject repoEnv data


  [<HttpGet>]
  member this.GetAll() =

    task {
      let! result = getProjectVersionsWithState (repoEnv, actorFactory)

      match result with
      | Ok result ->
        return result |> Ok
      | Error err -> return err |> Error
    }

  [<HttpDelete("{projectId}")>]
  member this.Delete(projectId: string) = repo.Delete projectId
