﻿namespace Common.DaprState

[<AutoOpen>]
module State =

  open Dapr.Client
  open Microsoft.Extensions.Logging

  type UpdateResult<'a> =
    { IsSuccess: bool
      ETag: string
      Id: string
      Doc: 'a }

  let private NEW_ETAG = "-1"

  type StateEnv =
    { Logger: ILogger
      StateManager: IStateManager
      StoreName: string }

  /// Create new item or fail if already exists
  let tryCreateStateWithMetadataAsync
    { Logger = logger
      StateManager = stateManager
      StoreName = storeName }
    id
    doc
    metadata
    =
    task {
      let! res = stateManager.trySave (storeName, id, doc, NEW_ETAG, metadata = metadata)

      match res with
      | true -> logger.LogTrace("{stateStore} updated with new {document}", storeName, "[doc]")
      | false -> logger.LogWarning("{stateStore} failed to update, {docKey} already exists", storeName, id)

      return res
    }

  let tryCreatStateAsync opts id doc =
    tryCreateStateWithMetadataAsync opts id doc (readOnlyDict [])

  // Create new item even if exists
  let createStateWithMetadataAsync
    { Logger = logger
      StateManager = stateManager
      StoreName = storeName }
    id
    doc
    metadata
    =
    task {
      do! stateManager.save (storeName, id, doc, metadata = metadata)

      logger.LogTrace("{stateStore} record with {id} updated with new {document}", storeName, id, "[doc]")
    }

  let creatStateTTLAsync opts id doc (ttl: int) =
    createStateWithMetadataAsync opts id doc (readOnlyDict [ "ttlInSeconds", ttl.ToString() ])

  let creatStatePartitionAsync opts id doc (partitionKey: string) =
    createStateWithMetadataAsync opts id doc (readOnlyDict [ "partitionKey", partitionKey ])

  let creatStateAsync opts id doc =
    createStateWithMetadataAsync opts id doc (readOnlyDict [])

  /// Find item and update it if exists
  /// If item is not exists then create new and then update it
  let tryUpdateOrCreateStateAsync<'a, 'b>
    { Logger = logger
      StateManager = stateManager
      StoreName = storeName }
    id
    (updateFun: 'a option -> Result<'a, 'b>)
    =
    task {
      let! docEntry = stateManager.getEntry<'a> (storeName, id)

      let (etag, doc) =
        match box docEntry.Value with
        | null ->
          // document still not created
          (NEW_ETAG, updateFun None)
        | _ -> (docEntry.ETag, updateFun (Some docEntry.Value))

      match doc with
      | Ok doc ->
        let! res = stateManager.trySave (storeName, id, doc, etag)

        let res =
          { IsSuccess = res
            ETag = etag
            Id = id
            Doc = doc }

        match res.IsSuccess with
        | true -> logger.LogTrace("{stateStore} document with {docKey} is updated with {result}", storeName, id, res)
        | false ->
          logger.LogTrace("{stateStore} document with {docKey} fail to update with {etag}", storeName, id, etag)

        return Ok doc
      | Error err ->
        logger.LogTrace("{stateStore} document with {docKey} update is skipped due to {@err}", storeName, id, err)
        return Error err
    }

  let tryUpdateStateAsync'<'a>
    { Logger = logger
      StateManager = stateManager
      StoreName = storeName }
    id
    (updateFun: 'a -> 'a option)
    =
    task {
      let! docEntry = stateManager.getEntry<'a> (storeName, id)

      let result =
        match box docEntry.Value with
        | null -> None
        | _ -> Some(docEntry.ETag, docEntry.Value)

      match result with
      | Some (etag, doc') ->
        let doc = updateFun doc'

        match doc with
        | Some doc ->
          let! res = stateManager.trySave (storeName, id, doc, etag)

          let res =
            { IsSuccess = res
              ETag = etag
              Id = id
              Doc = doc }

          match res.IsSuccess with
          | true ->
            logger.LogTrace("{stateStore} document with {docKey} is updated with {result}", storeName, id, "[res]")
          | false ->
            logger.LogWarning("{stateStore} document with {docKey} fail to update with {etag}", storeName, id, etag)

          return Some(doc, doc')
        | None -> return None
      | None -> return None
    }

  let tryUpdateStateAsync<'a> env id updateFun =
    task {
      let! result = tryUpdateStateAsync'<'a> env id (updateFun >> Some)
      return result |> Option.map (fst)
    }


  let getStateAsync<'a>
    { Logger = logger
      StateManager = stateManager
      StoreName = storeName }
    id
    =
    task {

      let! res = stateManager.get<'a> (storeName, id)

      match box res with
      | null ->
        logger.LogWarning("{stateStore} get value for {id} not found", storeName, id)
        return None
      | _ ->
        logger.LogTrace("{stateStore} get value for {id} return {res}", storeName, id, "[res]")
        return Some res

    }

  let getStateListAsync<'a> opts id =
    task {
      let! result = getStateAsync<'a list> opts id

      return
        match result with
        | Some list -> list
        | None -> []
    }

  /// Create new item or fail if already exists
  let deleteStateAsync
    { Logger = logger
      StateManager = stateManager
      StoreName = storeName }

    id
    =
    stateManager.delete (storeName, id)

  let getStateAndRemoveAsync<'a> ctx id =
    task {
      let! state = getStateAsync<'a> ctx id

      match state with
      | Some _ -> do! deleteStateAsync ctx id
      | None -> ()

      return state
    }
