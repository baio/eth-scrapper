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
  do 
    printfn "+++ %O" repoEnv

  [<HttpPost>]
  member this.Post(data: CreateProjectEntity) = createProject repoEnv data

  [<HttpGet>]
  member this.GetAll() =
    getProjectVersionsWithState (repoEnv, actorFactory)

  [<HttpDelete("{projectId}")>]
  member this.Delete(projectId: string) = repo.Delete projectId
