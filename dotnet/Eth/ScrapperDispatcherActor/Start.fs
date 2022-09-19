namespace ScrapperDispatcherActor

[<AutoOpen>]
module internal Start =

  open Dapr.Actors
  open ScrapperModels
  open Microsoft.Extensions.Logging


  let start ((runScrapperEnv, env): RunScrapperEnv * ActorEnv) (data: StartData) =

    let logger = env.Logger

    logger.LogDebug("Start with {@data}", data)

    task {
      let! state = env.GetState()
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

        return! runScrapper runScrapperEnv env.ActorId Start scrapperRequest
    }
