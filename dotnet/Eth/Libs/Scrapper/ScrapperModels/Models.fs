namespace ScrapperModels

type BlockRange = { From: uint; To: uint }

type Success =
  { BlockRange: BlockRange
    ItemsCount: uint }

type ErrorData =
  | EmptyResult
  | LimitExceeded
  | Unknown

type Error =
  { Data: ErrorData
    BlockRange: BlockRange }

type ScrapperResult = Result<Success, Error>

type ScrapperRequest =
  { EthProviderUrl: string
    ContractAddress: string
    Abi: string
    BlockRange: BlockRange }

[<RequireQualifiedAccess>]
type AppId =
  | Dispatcher
  | ElasticStore
  | Scrapper
  | JobManager

type JobManagerId = JobManagerId of string
type JobId = JobId of string
