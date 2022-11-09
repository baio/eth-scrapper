module LimitExceedsContinueJobTests

open Expecto
open ScrapperModels
open Common.Utils.Task
open Common.Utils
open ScrapperTestContext
open System.Threading

[<Tests>]
let tests =
  let ethBlocksCount = 1000u
  let maxEthItemsInResponse = 100u
  let semaphore = new SemaphoreSlim(0, 1)

  let onScrap: OnScrap =
    fun request ->
      task {
        let result: ScrapperModels.ScrapperResult =
          { Data = EmptyResult
            BlockRange = request.BlockRange }
          |> Error

        return result
      }


  let date = System.DateTime.UtcNow

  let env =
    { EthBlocksCount = ethBlocksCount
      MaxEthItemsInResponse = maxEthItemsInResponse
      OnScrap = onScrap
      OnReportJobStateChanged = releaseOnSuccess semaphore
      Date = fun () -> date }

  let context = Context env

  let expected: ScrapperDispatcher.State =
    { Status = ScrapperDispatcher.Status.Finish
      Request =
        { EthProviderUrl = "http://test"
          ContractAddress = ""
          Abi = ""
          BlockRange = { From = 0u; To = ethBlocksCount } }
      Date = date |> toEpoch
      FinishDate = date |> toEpoch |> Some
      ItemsPerBlock = []
      Target =
        { ToLatest = true
          Range = { From = 0u; To = ethBlocksCount } }
      ParentId = None }: ScrapperDispatcher.State


  testCaseAsync
    "job: when scrapper returns limit exceeds the job should continue"
    (task {

      let jobId = JobId "1"
      let job = context.createScrapperDispatcher jobId

      let startData: ScrapperDispatcher.StartData =
        { EthProviderUrl = "http://test"
          ContractAddress = ""
          Abi = ""
          Target = None
          ParentId = None }

      let! _ = job.Start(startData)

      let! _ = semaphore.WaitAsync 100

      let! jobManangerState = context.JobMap.GetItem jobId

      Expect.equal jobManangerState (Some expected) "job state is not expected"

     }
     |> Async.AwaitTask)
