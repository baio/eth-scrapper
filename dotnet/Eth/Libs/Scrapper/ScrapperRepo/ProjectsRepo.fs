namespace Scrapper.Repo

open ScrapperModels


module PeojectsRepo =
  open Common.DaprState.StateList
  open Common.Repo
  open Common.Utils

  type CreateProjectEntity =
    { Name: string
      Address: string
      Abi: string
      EthProviderUrl: string
      VersionId: string }

  type ProjectEntity =
    { Id: string
      Name: string
      Address: string
      Abi: string
      EthProviderUrl: string }

  type VersionWithState =
    { Version: VersionEntity
      State: State option }

  type ProjectWithVresionsAndState =
    { Project: ProjectEntity
      Versions: VersionWithState list }

  type ProjectWithVresions =
    { Project: ProjectEntity
      Versions: VersionEntity list }


  // TODO : Multi user env
  let USER_KEY = "user_USER_KEY_projects"

  let createRepo env =
    let repo = stateListRepo<ProjectEntity> env
    let versionRepo = ProjectVersionsRepo.createRepo env

    let getOne projId =
      repo.GetHead USER_KEY (fun x -> x.Id = projId)

    {| Create =
        fun (createEnty: CreateProjectEntity) ->
          task {
            let enty =
              { Id = createEnty.Address
                Name = createEnty.Name
                Address = createEnty.Address
                Abi = createEnty.Abi
                EthProviderUrl = createEnty.EthProviderUrl }

            let! result = repo.Insert USER_KEY (fun x -> x.Id = enty.Id) enty

            match (result) with
            | (Ok proj) ->
              if createEnty.VersionId <> null then
                let version: CreateVersionEntity = { Id = createEnty.VersionId }
                let! version = versionRepo.Create proj.Id version

                match version with
                | Ok version ->
                  return
                    { Project = proj
                      Versions = [ version ] }
                    |> Ok
                | Error _ -> return { Project = proj; Versions = [] } |> Ok
              else
                return { Project = proj; Versions = [] } |> Ok
            | Error err -> return err |> box |> Conflict |> Error
          }
       GetAll = fun () -> repo.GetAll USER_KEY |> taskMap Ok
       GetAllWithVerions =
        fun () ->
          task {
            let! projects = repo.GetAll USER_KEY

            let versions =
              projects
              |> List.map (fun proj ->
                task {
                  let! result = versionRepo.GetAll proj.Id

                  match result with
                  | Ok result -> return { Project = proj; Versions = result }
                  | Error _ -> return { Project = proj; Versions = [] }
                })

            let! result = versions |> Task.all

            return result |> Ok
          }
       GetOneWithVersion =
        fun projId verId ->
          task {
            let! proj = getOne projId

            match proj with
            | Some proj ->
              let! ver = versionRepo.GetOne projId verId

              match ver with
              | Some ver -> return Some(proj, ver)
              | None -> return None
            | None -> return None
          }
          |> taskMap noneToNotFound
       Update =
        fun id enty ->
          repo.Update USER_KEY (fun enty -> enty.Id = id) (fun _ -> enty)
          |> taskMap noneToNotFound
       Delete =
        fun id ->
          task {
            let! result = repo.Delete USER_KEY (fun enty -> enty.Id = id)

            match result with
            | Some _ -> do! versionRepo.DeleteAll id
            | None -> ()

            return result |> noneToNotFound
          }
       CreateVersion =
        fun id ver ->
          task {
            let! proj = getOne id

            match proj with
            | Some _ -> return! versionRepo.Create id ver
            | None -> return RepoError.NotFound |> Error
          }
       DeleteVersion = fun id verId -> versionRepo.Delete id verId
       GetAllVersions = fun id -> versionRepo.GetAll id |}
