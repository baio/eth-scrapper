namespace ScrapperDispatcherActor

open ScrapperModels.ScrapperDispatcher

[<AutoOpen>]
module internal Start =

  open Dapr.Actors
  open ScrapperModels
  open Microsoft.Extensions.Logging

  let private getTargetBlockRange (env: Env) ethProviderUrl (target: TargetBlockRange option) =
    task {
      match target with
      | Some target -> return target.ToLatest, target.Range
      | None ->
        let! ethBlocksCount = env.GetEthBlocksCount ethProviderUrl
        return true, { From = 0u; To = ethBlocksCount }
    }

  let start (env: Env) (data: StartData) =

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
        let! (toLatest, blockRange) = getTargetBlockRange env data.EthProviderUrl data.Target

        let scrapperRequest: ScrapperRequest =
          { EthProviderUrl = data.EthProviderUrl
            ContractAddress = data.ContractAddress
            Abi = data.Abi
            BlockRange = blockRange }

        return! runScrapperStart env (data.ParentId, toLatest) scrapperRequest
    }
