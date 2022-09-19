namespace ScrapperModels

open Dapr.Actors
open System.Threading.Tasks

type ContinueSuccessResult =
  { BlockRange: BlockRange
    IndexPayload: string
    ItemsCount: uint }

type ContinueSuccessData =
  { EthProviderUrl: string
    ContractAddress: string
    Abi: string
    Result: ContinueSuccessResult }

type IScrapperStoreActor =
  inherit IActor
  abstract Store: data: ContinueSuccessData -> Task<bool>
