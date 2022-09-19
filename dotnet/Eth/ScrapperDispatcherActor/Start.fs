namespace ScrapperDispatcherActor

[<AutoOpen>]
module internal Start =

  open Dapr.Actors
  open Dapr.Actors.Runtime
  open System.Threading.Tasks
  open ScrapperModels
  open Microsoft.Extensions.Logging
  open Common.DaprActor
  open Common.DaprActor.ActorResult
  open System
  open Nethereum.Web3


  let start ((env, actorId): RunScrapperEnv * ActorId) state (data: StartData) =

    let logger = env.Logger

    logger.LogDebug("Start with {@data} {@state}", data, state)

    task {

      match state with
      | Some state ->
        let error = "Trying to start version which already started"
        logger.LogError(error, data)
        return (state, error) |> StateConflict |> Error
      | None ->
        let! ethBlocksCount = getEthBlocksCount data.EthProviderUrl

        let scrapperRequest: ScrapperRequest =
          { EthProviderUrl = data.EthProviderUrl
            ContractAddress = data.ContractAddress
            Abi = data.Abi
            BlockRange = { From = 0u; To = ethBlocksCount } }

        return! runScrapper env actorId Start scrapperRequest
    }
