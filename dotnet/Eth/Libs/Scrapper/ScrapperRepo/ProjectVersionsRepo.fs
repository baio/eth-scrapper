namespace Scrapper.Repo

type CreateVersionEntity = { Id: string }

type VersionEntity =
  { Id: string
    Created: System.DateTime }

module internal ProjectVersionsRepo =
  open Common.DaprState.StateList
  open Common.Repo.RepoResult

  let getKey projId = $"project_{projId}_versions"

  let createRepo env =
    let repo = stateListRepo<VersionEntity> env

    {| Create =
        fun projId (ver: CreateVersionEntity) ->
          let key = getKey projId          
          repo.Insert
            key
            (fun x -> x.Id = ver.Id)
            { Id = ver.Id
              Created = System.DateTime.UtcNow }
          |> taskMap errorToConflict
       GetAll = fun projId -> repo.GetAll(getKey projId) |> taskMap Ok
       GetOne = fun projId verId -> repo.GetHead (getKey projId) (fun enty -> enty.Id = verId)
       Delete =
        fun projId id ->
          repo.Delete (getKey projId) (fun enty -> enty.Id = id)
          |> taskMap noneToNotFound
       DeleteAll = fun projId -> repo.DeleteAll(getKey projId) |}
