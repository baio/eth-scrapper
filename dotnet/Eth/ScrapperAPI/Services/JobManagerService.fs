namespace ScrapperAPI.Services

open ScrapperModels

module JobManagerService =

  open Dapr.Actors
  open Dapr.Actors.Client
  open Scrapper.Repo.PeojectsRepo
  open Common.DaprState
  open Common.Repo
  open Common.Utils
  open ScrapperModels.JobManager


  let private getActorId projectId versionId =
    let actorId = $"{projectId}_{versionId}"
    ActorId actorId

  let private createActor projectId versionId =
    let actorId = getActorId projectId versionId

    ActorProxy.Create<IJobManagerActor>(actorId, "job-manager")

  let state projectId versionId =

    let actor = createActor projectId versionId

    actor.State()

  let pause projectId versionId =

    let actor = createActor projectId versionId

    actor.Pause()

  let resume projectId versionId =

    let actor = createActor projectId versionId

    actor.Resume()

  let reset projectId versionId =

    let actor = createActor projectId versionId

    actor.Reset()

  type StartError =
    | ActorFailure of Error
    | RepoError of RepoError

  let start (env: DaprStoreEnv) projectId versionId =
    let repo = createRepo env

    task {

      let! result = repo.GetOneWithVersion projectId versionId

      match result with
      | Ok (proj, ver) ->
        let actor = createActor projectId ver.Id

        let data: StartData =
          { EthProviderUrl = proj.EthProviderUrl
            ContractAddress = proj.Address
            Abi = proj.Abi }

        let! result = actor.Start data

        match result with
        | Ok result -> return result |> Ok
        | Error err -> return err |> ActorFailure |> Error

      | Error err -> return err |> RepoError |> Error

    }

  let collectProjectVersionsWithState (projects: ProjectWithVersions list) =

    projects
    |> List.map (fun proj ->
      task {
        let! result =
          proj.Versions
          |> List.map (fun v ->
            task {
              try
                let! st = state proj.Project.Id v.Id
                return (v, st)
              with
              | _ -> return (v, None)
            })
          |> Task.all

        let result = (proj, result)

        return result
      })
    |> Task.all
