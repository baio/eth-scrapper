namespace ScrapperAPI.Services

module ScrapperDispatcherService =

  open Dapr.Actors
  open Dapr.Actors.Client
  open ScrapperModels
  open Scrapper.Repo.PeojectsRepo
  open Common.DaprState
  open Common.Repo
  open Common.Utils

  let private getActorId projectId versionId =
    let actorId = $"{projectId}_{versionId}"
    ActorId actorId

  let private createActor projectId versionId =
    let actorId = getActorId projectId versionId

    ActorProxy.Create<IScrapperDispatcherActor>(actorId, "scrapper-dispatcher")

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

  let collectProjectVersionsWithState (projects: ProjectWithVresions list) =

    projects
    |> List.map (fun proj ->
      task {
        let! result =
          proj.Versions
          |> List.map (fun v ->
            task {
              try
                let! st = state proj.Project.Id v.Id
                return { Version = v; State = st }
              with
              | _ -> return { Version = v; State = None }
            })
          |> Task.all

        let result: ProjectWithVresionsAndState =
          { Project = proj.Project
            Versions = result }

        return result
      })
    |> Task.all


  type StartError =
    | ActorFailure of ScrapperDispatcherActorError
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
            Abi = proj.Abi
            Target = None }

        let! result = actor.Start data

        match result with
        | Ok result -> return result |> Ok
        | Error err -> return err |> ActorFailure |> Error

      | Error err -> return err |> RepoError |> Error

    }
