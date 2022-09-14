namespace ScrapperAPI.Controllers

open Microsoft.AspNetCore.Mvc
open Common.DaprState
open Scrapper.Repo
open Scrapper.Repo.PeojectsRepo
open ScrapperAPI.Services.ScrapperDispatcherService
open Common.DaprAPI

[<ApiController>]
[<Route("projects/{projectId}/versions")>]
type ProjectVersionssController(env: DaprStoreEnv) =
  inherit ControllerBase()
  let repo = createRepo env

  [<HttpPost>]
  member this.Post(projectId: string, data: CreateVersionEntity) = repo.CreateVersion projectId data

  [<HttpGet>]
  member this.GetAll(projectId: string) = repo.GetAllVersions projectId

  [<HttpDelete("{versionId}")>]
  member this.Delete(projectId: string, versionId: string) = repo.DeleteVersion projectId versionId

  [<HttpPost("{versionId}/start")>]
  member this.Start(projectId: string, versionId: string) =
    task {
      let! result = start env projectId versionId

      match result with
      | Error err ->
        match err with
        | ActorFailure err -> return err |> DTO.mapScrapperDispatcherActorError
        | RepoError err -> return mapRepoError err
      | Ok result -> return result |> DTO.mapScrapperDispatcherActorSuccess
    }

  [<HttpGet("{versionId}/state")>]
  member this.State(projectId: string, versionId: string) =
    task {
      let! result = state projectId versionId

      match result with
      | Some result -> return result |> DTO.mapState |> this.Ok :> IActionResult
      | None -> return NotFoundObjectResult() :> IActionResult
    }

  [<HttpPost("{versionId}/pause")>]
  member this.Pause(projectId: string, versionId: string) =
    task {
      let! result = pause projectId versionId
      return DTO.mapScrapperDispatcherActorResult result
    }

  [<HttpPost("{versionId}/resume")>]
  member this.Resume(projectId: string, versionId: string) =
    task {
      let! result = resume projectId versionId
      return DTO.mapScrapperDispatcherActorResult result
    }

  [<HttpPost("{versionId}/reset")>]
  member this.Reset(projectId: string, versionId: string) =
    task {
      let! result = reset projectId versionId

      match result with
      | true -> return NoContentResult() :> IActionResult
      | false -> return NotFoundObjectResult() :> IActionResult
    }
