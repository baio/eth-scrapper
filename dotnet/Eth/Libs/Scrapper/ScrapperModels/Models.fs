namespace ScrapperModels

type BlockRange = { From: uint; To: uint }

type RequestBlockRange = { From: uint option; To: uint option }

type Success =
  { RequestBlockRange: RequestBlockRange
    BlockRange: BlockRange
    ItemsCount: uint }

type ErrorData =
  | EmptyResult
  | LimitExceeded
  | Unknown

type Error =
  { Data: ErrorData
    RequestBlockRange: RequestBlockRange
    BlockRange: BlockRange }

type ScrapperResult = Result<Success, Error>

type ScrapperRequest =
  { EthProviderUrl: string
    ContractAddress: string
    Abi: string
    BlockRange: RequestBlockRange }
