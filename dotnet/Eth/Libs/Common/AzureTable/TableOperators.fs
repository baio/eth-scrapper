namespace Infra.AzureTable

open Azure.Data.Tables
open FSharp.Control
open System.Text

[<AutoOpen>]
module TableOperators =

    let private jsonSerializerOptions =
        Json.JsonSerializerOptions(PropertyNameCaseInsensitive = true)

    let private serialize x =
        Json.JsonSerializer.Serialize<'T>(x, jsonSerializerOptions)

    let private deserialize<'T> (x: string) =
        Json.JsonSerializer.Deserialize<'T>(x, jsonSerializerOptions)

    type TableEntity with
        member __.Data
            with get () = __.GetString "Data"
            and set value = __.Add("Data", value)

    type ReadModelRow(partitionKey, rowKey, data) =
        let tableEntity = TableEntity(partitionKey, rowKey)
        do tableEntity.Data <- serialize data
        member _.Entity = tableEntity

    let private deserializeTableEntity<'T> (tableEntity: TableEntity) =
        tableEntity
        |> unbox<TableEntity>
        |> fun r -> deserialize<'T> r.Data

    let private partitionKeyEqFilter = sprintf "PartitionKey eq '%s'"

    let private partitionKeyAndIdEqFilter =
        sprintf "PartitionKey eq '%s' and RowKey eq '%s'"

    let private executeQuery<'T> (table: TableClient) (filter: string) =
        table.QueryAsync<TableEntity> filter
        |> AsyncSeq.ofAsyncEnum
        |> AsyncSeq.map (deserializeTableEntity<'T>)

    let getAllByPartitionKey<'T> (table: TableClient) partitionKey =
        let filter = partitionKeyEqFilter partitionKey
        executeQuery<'T> table filter

    let countByPartitionKey (table: TableClient) partitionKey =
        let filter = partitionKeyEqFilter partitionKey

        table.QueryAsync<TableEntity> filter
        |> AsyncSeq.ofAsyncEnum
        |> AsyncSeq.length

    let getEntityByPartitionAndId<'T> (table: TableClient) partitionKey id =
        let filter =
            partitionKeyAndIdEqFilter partitionKey id

        table.QueryAsync<TableEntity> filter
        |> AsyncSeq.ofAsyncEnum
        |> AsyncSeq.tryFirst


    let getByPartitionAndId<'T> (table: TableClient) partitionKey id =

        task {
            let! result = getEntityByPartitionAndId table partitionKey id

            let result =
                result |> Option.map deserializeTableEntity<'T>

            return result
        }

    let getTableEntity partition id readModel =
        let data = serialize readModel
        let entity = TableEntity(partition, id)
        entity.Data <- data
        entity

    let insertReadModel (table: TableClient) partition id readModel =
        task {
            let entity = getTableEntity partition id readModel
            let! _ = table.AddEntityAsync entity
            ()
        }

    let upsertReadModel (table: TableClient) partition id readModel =
        task {
            let entity = getTableEntity partition id readModel
            let! _ = table.UpsertEntityAsync(entity, TableUpdateMode.Replace)
            ()
        }

    let deleteByPartitionAndId (table: TableClient) partition id =
        task {
            let! _ = table.DeleteEntityAsync(partition, id)
            ()
        }

    let deleteByPartitionAndIds (table: TableClient) partition ids =
        ids
        |> AsyncSeq.ofSeq
        |> AsyncSeq.mapAsync (fun id ->
            task {
                try
                    do! deleteByPartitionAndId table partition id
                    return id |> Ok
                with
                | ex -> return (id, ex) |> Error
            }
            |> Async.AwaitTask)
        |> AsyncSeq.toListAsync
