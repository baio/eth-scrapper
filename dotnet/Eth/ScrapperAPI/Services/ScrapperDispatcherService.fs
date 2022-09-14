namespace ScrapperAPI.Services

module ScrapperDispatcherService =

  open Dapr.Actors
  open Dapr.Actors.Client
  open ScrapperModels
  open Scrapper.Repo.PeojectsRepo
  open Common.DaprState
  open Common.Repo
  open Common.Utils

  let private getActorId contractAddress versionId =
    let actorId = $"{contractAddress}_{versionId}"
    ActorId actorId

  let private createActor contractAddress versionId =
    let actorId = getActorId contractAddress versionId

    ActorProxy.Create<IScrapperDispatcherActor>(actorId, "scrapper-dispatcher")

  let state contractAddress versionId =

    let actor = createActor contractAddress versionId

    actor.State()

  let pause contractAddress versionId =

    let actor = createActor contractAddress versionId

    actor.Pause()

  let resume contractAddress versionId =

    let actor = createActor contractAddress versionId

    actor.Resume()

  let reset contractAddress versionId =

    let actor = createActor contractAddress versionId

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
                let! st = state proj.Project.Address v.Id
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
            Abi = proj.Abi }

        let! result = actor.Start data

        match result with
        | Ok result -> return result |> Ok
        | Error err -> return err |> ActorFailure |> Error

      | Error err -> return err |> RepoError |> Error

    }
