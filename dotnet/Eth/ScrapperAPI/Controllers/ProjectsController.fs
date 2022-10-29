namespace ScrapperAPI.Controllers

open Microsoft.AspNetCore.Mvc
open Microsoft.Extensions.Logging
open Microsoft.AspNetCore.Authorization
open Common.DaprState
open Scrapper.Repo.PeojectsRepo
open ScrapperAPI.Services.JobManagerService
open Dapr.Abstracts

[<ApiController>]
[<Route("projects")>]
type ProjectsController(stateEnv: StateEnv, actorFactory: IActorFactory) =
  inherit ControllerBase()
  let repo = createRepo stateEnv

  [<HttpPost>]
  member this.Post(data: CreateProjectEntity) = repo.Create data


  [<HttpGet>]
  member this.GetAll() =

    task {
      let! result = getProjectVersionsWithState (stateEnv, actorFactory)

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
