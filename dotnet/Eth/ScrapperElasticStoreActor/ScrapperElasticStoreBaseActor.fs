namespace ScrapperElasticStoreActor

open System.Threading.Tasks

open Dapr.Actors
open ScrapperModels.ScrapperDispatcher
open ScrapperModels.ScrapperStore
open Microsoft.Extensions.Logging
open ScrapperModels

type Env =
  { Logger: ILogger
    ActorId: JobId
    CreateScrapperDispatcherActor: JobId -> IScrapperDispatcherActor
    Store: string -> Task<bool> }

[<AutoOpen>]
module ScrapperStoreBaseActor =

  let runScrapperDispatcherContinue (env: Env) (data: ContinueSuccessData) =
    let actor = env.CreateScrapperDispatcherActor env.ActorId

    let success = data.Result

    let continueData: ContinueData =
      { EthProviderUrl = data.EthProviderUrl
        ContractAddress = data.ContractAddress
        Abi = data.Abi
        Result =
          (Ok
            { BlockRange = success.BlockRange
              ItemsCount = success.ItemsCount }) }

    actor.Continue continueData |> ignore


  let runScrapperDispatcherFailure (env: Env) =
    let actor = env.CreateScrapperDispatcherActor env.ActorId

    let failureData: FailureData =
      { AppId = AppId.ElasticStore
        Status = StoreFailure "Failed to store in elasticsearch" }

    actor.Failure failureData |> ignore

  type ScrapperElasticStoreBaseActor(env: Env) =

    interface IScrapperStoreActor with
      member this.Store data =
        task {
          let! result = env.Store data.Result.IndexPayload

          match result with
          | true -> runScrapperDispatcherContinue env data
          | false -> runScrapperDispatcherFailure env

          return result
        }
