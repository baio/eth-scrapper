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

  [<HttpGet>]
  member this.GetAll() : System.Threading.Tasks.Task<Result<list<ProjectWithVresionsAndState>, obj>> =
    getProjectVersionsWithState (repoEnv, actorFactory)

  [<HttpPost>]
  member this.Post(data: CreateProjectEntity) = createProject repoEnv data

  [<HttpDelete("{projectId}")>]
  member this.Delete(projectId: string) =
    deleteProject (repoEnv, actorFactory) projectId
