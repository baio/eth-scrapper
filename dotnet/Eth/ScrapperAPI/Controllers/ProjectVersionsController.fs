namespace ScrapperAPI.Controllers

open Microsoft.AspNetCore.Mvc
open Common.DaprState
open Scrapper.Repo
open Scrapper.Repo.PeojectsRepo
open ScrapperAPI.Services.JobManagerService
open Common.DaprAPI
open JobsManagerDTO

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
      | Error err ->
        match err with
        | ActorFailure err -> return err |> mapError
        | RepoError err -> return mapRepoError err
      | Ok result -> return result |> mapSuccess
    }

  [<HttpGet("{versionId}/state")>]
  member this.State(projectId: string, versionId: string) =
    task {
      let! result = state actorFactory projectId versionId

      match result with
      | Some result -> return result |> this.Ok :> IActionResult
      | None -> return NotFoundObjectResult() :> IActionResult
    }

  [<HttpPost("{versionId}/pause")>]
  member this.Pause(projectId: string, versionId: string) =
    task {
      let! result = pause actorFactory projectId versionId
      return mapResult result
    }

  [<HttpPost("{versionId}/resume")>]
  member this.Resume(projectId: string, versionId: string) =
    task {
      let! result = resume actorFactory projectId versionId
      return mapResult result
    }

  [<HttpPost("{versionId}/reset")>]
  member this.Reset(projectId: string, versionId: string) =
    task {
      let! result = reset actorFactory projectId versionId

      match result with
      | Ok _ -> return NoContentResult() :> IActionResult
      | Error _ -> return NotFoundObjectResult() :> IActionResult
    }
