namespace ScrapperAPI.Services

module JobManagerService =

  open Dapr.Actors
  open Scrapper.Repo.PeojectsRepo
  open Common.DaprState
  open Common.Repo
  open Common.Utils
  open Scrapper.Repo
  open ScrapperModels
  open ScrapperModels.JobManager

  type JobManagerActorFactory = JobManagerId -> IJobManagerActor

  let private getActorId projectId versionId =
    let actorId = $"{projectId}_{versionId}"
    actorId

  let private createActor (factory: JobManagerActorFactory) projectId versionId =
    getActorId projectId versionId
    |> JobManagerId
    |> factory

  let state factory projectId versionId =

    let actor = createActor factory projectId versionId

    actor.State()

  let pause factory projectId versionId =

    let actor = createActor factory projectId versionId

    actor.Pause()

  let resume factory projectId versionId =

    let actor = createActor factory projectId versionId

    actor.Resume()

  let reset factory projectId versionId =

    let actor = createActor factory projectId versionId

    actor.Reset()

  type StartError =
    | ActorFailure of Error
    | RepoError of RepoError


  type VersionWithState =
    { Version: VersionEntity
      State: State option }

  type ProjectWithVresionsAndState =
    { Project: ProjectEntity
      Versions: VersionWithState list }


  let createProject repoEnv data =
    let repo = createRepo repoEnv
    repo.Create data

  let start ((repoEnv, factory): RepoEnv * JobManagerActorFactory) projectId versionId =
    let repo = createRepo repoEnv

    task {

      let! result = repo.GetOneWithVersion projectId versionId

      match result with
      | Ok (proj, ver) ->
        let actor = createActor factory projectId ver.Id

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

  let getProjectVersionsWithState ((repoEnv, factory): RepoEnv * JobManagerActorFactory) =
    let repo = createRepo repoEnv

    task {
      let! projects = repo.GetAllWithVerions()


      match projects with
      | Ok projects ->

        let! result =
          projects
          |> List.map (fun proj ->
            task {
              let! result =
                proj.Versions
                |> List.map (fun v ->
                  task {
                    try
                      let! st = state factory proj.Project.Id v.Id
                      return (v, st)
                    with
                    | _ -> return (v, None)
                  })
                |> Task.all

              let result: ProjectWithVresionsAndState =
                { Project = proj.Project
                  Versions =
                    result
                    |> List.map (fun (v, s) -> { Version = v; State = s }) }

              return result
            })
          |> Task.all

        return result |> Ok
      | Error err -> return err |> Error
    }
