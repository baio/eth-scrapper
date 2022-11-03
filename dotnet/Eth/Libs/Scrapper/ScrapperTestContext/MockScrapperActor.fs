namespace ScrapperTestContext

open Microsoft.Extensions.Logging
open ScrapperModels
open System.Threading.Tasks

type OnScrap = ScrapperModels.ScrapperRequest -> ScrapperResult

type ScrapperActorEnv =
  { Logger: ILogger
    ActorId: JobId
    CreateScrapperDispatcherActor: JobId -> ScrapperModels.ScrapperDispatcher.IScrapperDispatcherActor
    CreateStoreActor: JobId -> ScrapperModels.ScrapperStore.IScrapperStoreActor
    OnScrap: OnScrap }

type ScrapperActor(env: ScrapperActorEnv) =

  interface ScrapperModels.Scrapper.IScrapperActor with
    member this.Scrap data =
      let result = env.OnScrap data

      task {
        env.Logger.LogDebug("Scrap result {@result}", result)

        match result with
        | Ok result ->
          let data: ScrapperModels.ScrapperStore.ContinueSuccessData =
            { EthProviderUrl = data.EthProviderUrl
              Abi = data.Abi
              ContractAddress = data.ContractAddress
              Result =
                { BlockRange = result.BlockRange
                  ItemsCount = result.ItemsCount
                  IndexPayload = "test payload" } }

          let actor = env.CreateStoreActor(env.ActorId)

          task {
            // imitate waiting
            do! Task.Delay(1) |> Async.AwaitTask
            let! _ = actor.Store data
            return ()
          }
          |> ignore

          return true
        | Error _ ->
          let data: ScrapperModels.ScrapperDispatcher.ContinueData =
            { EthProviderUrl = data.EthProviderUrl
              Abi = data.Abi
              ContractAddress = data.ContractAddress
              Result = result }

          let actor = env.CreateScrapperDispatcherActor(env.ActorId)

          task {
            // imitate waiting
            do! Task.Delay(1) |> Async.AwaitTask
            let! _ = actor.Continue data
            return ()
          }
          |> ignore

          return true
      }
