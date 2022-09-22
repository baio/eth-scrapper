namespace JobManagerActor

[<AutoOpen>]
module internal GetEthBlocksCount =

  open Nethereum.Web3

  let getEthBlocksCount (ethProviderUrl: string) =
    task {
      let web3 = new Web3(ethProviderUrl)
      let! result = web3.Eth.Blocks.GetBlockNumber.SendRequestAsync()
      return result.Value |> uint
    }
