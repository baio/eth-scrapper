namespace ScrapperActor

open Nethereum.ABI.FunctionEncoding.Attributes

[<AutoOpen>]
module ScrapperActor =

  open Dapr.Actors
  open Dapr.Actors.Runtime
  open System.Threading.Tasks
  open ScrapperModels
  open Nethereum.Web3

  [<Event("*", true)>]
  type EventDTO() =
    interface IEventDTO with



  [<Actor(TypeName = "scrapper")>]
  type ScrapperActor(host: ActorHost) =
    inherit Actor(host)

    let getBlockNumber (web3: Web3) =
      task {
        let! blockNumber = web3.Eth.Blocks.GetBlockNumber.SendRequestAsync()
        return blockNumber.Value |> uint
      }

    let getToBlock web3 (toBlock: uint option) =
      task {
        match toBlock with
        | Some x -> return x
        | None -> return! getBlockNumber web3
      }

    interface IScrapperActor with
      member this.Scrap request =

        task {

          try
            let url = "https://mainnet.infura.io/v3/11f741ef96ad4ce5b963ea0ba8d9703b"
            let web3 = Web3 url
            let fromBlock = request.BlockRange.From |> Option.defaultValue 0u
            let! toBlock = getToBlock web3 request.BlockRange.To

            let transferEventHandler = web3.Eth.GetEvent<EventDTO>(request.ContractAddress)

            let filter = transferEventHandler.CreateFilterInput(fromBlock, toBlock)
            
            let! events = transferEventHandler.GetAllChangesAsync(filter)

            return
              { Data = EmptyResult
                BlockRange = { From = fromBlock; To = toBlock } }
              |> Error
          with
          | _ as err ->
            printfn "as err ??? %O" err

            return
              { Data = Unknown
                BlockRange = { From = 0u; To = 0u } }
              |> Error

        }
