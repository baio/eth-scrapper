namespace ScrapperAPI.Controllers

open Microsoft.AspNetCore.Mvc
open Scrapper.Repo
open Scrapper.Repo.PeojectsRepo
open ScrapperAPI.Services.JobManagerService
open ScrapperModels.JobManager

[<ApiController>]
[<Route("projects/{projectId}/versions")>]
type ProjectVersionssController(repoEnv: RepoEnv, actorFactory: JobManagerActorFactory) =
  inherit ControllerBase()
  let repo = createRepo repoEnv

  [<HttpPost>]
  member this.Post(projectId: string, data: CreateVersionEntity) = repo.CreateVersion projectId data

  [<HttpGet>]
  member this.GetAll(projectId: string) = repo.GetAllVersions projectId

  [<HttpDelete("{versionId}")>]
  member this.Delete(projectId: string, versionId: string) = repo.DeleteVersion projectId versionId

  [<HttpPost("{versionId}/start")>]
  member this.Start(projectId: string, versionId: string) =
    task {
     let! result = start (repoEnv, actorFactory) projectId versionId
     match result with
     | Ok result -> return Ok result
     | Error err ->
       match err with
       | StartError.ActorFailure err -> 
         return err |> box |> Error
       | StartError.RepoError err -> return err |> box |> Error
    }

  [<HttpGet("{versionId}/state")>]
  member this.State(projectId: string, versionId: string) = state actorFactory projectId versionId

  [<HttpPost("{versionId}/pause")>]
  member this.Pause(projectId: string, versionId: string) = pause actorFactory projectId versionId

  [<HttpPost("{versionId}/resume")>]
  member this.Resume(projectId: string, versionId: string) = resume actorFactory projectId versionId

  [<HttpPost("{versionId}/reset")>]
  member this.Reset(projectId: string, versionId: string) = reset actorFactory projectId versionId
