module EmptyResultFinishJobTests

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

  let mutable scrapCnt = 0
  let semaphore = new SemaphoreSlim(0, 3)
  let semaphore2 = new SemaphoreSlim(0, 1)

  let onScrap: OnScrap =
    fun request ->
      task {
        semaphore.Release() |> ignore

        return
          match scrapCnt with
          | 0 ->
            scrapCnt <- scrapCnt + 1

            { Data = LimitExceeded
              BlockRange = request.BlockRange }
            |> Error: ScrapperModels.ScrapperResult
          | 1 ->
            scrapCnt <- scrapCnt + 1

            { ItemsCount = 10u
              BlockRange = request.BlockRange }
            |> Ok: ScrapperModels.ScrapperResult
          | 2 ->
            scrapCnt <- scrapCnt + 1

            { Data = EmptyResult
              BlockRange = request.BlockRange }
            |> Error: ScrapperModels.ScrapperResult
          | _ -> failwith "not expected"
      }

  let date = System.DateTime.UtcNow

  let env =
    { EthBlocksCount = ethBlocksCount
      MaxEthItemsInResponse = maxEthItemsInResponse
      OnScrap = onScrap
      OnReportJobStateChanged = releaseOnSuccess semaphore2
      Date = fun () -> date }

  let context = Context env

  let expected: ScrapperDispatcher.State =
    { Status = ScrapperDispatcher.Status.Finish
      Request =
        { EthProviderUrl = "http://test"
          ContractAddress = ""
          Abi = ""
          BlockRange =
            { From = ethBlocksCount / 2u
              To = 5000u } } // itemsPerBlock * blockCount
      Date = date |> toEpoch
      FinishDate = date |> toEpoch |> Some
      ItemsPerBlock = [ 0.02 ]
      Target =
        { ToLatest = true
          Range = { From = 0u; To = ethBlocksCount } }
      ParentId = None }: ScrapperDispatcher.State

  testCaseAsync
    "job: LimitExceeded,  OK 10, EmptyResult finish"
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

      do! semaphore.WaitAsync()
      do! semaphore.WaitAsync()
      do! semaphore.WaitAsync()

      Expect.equal scrapCnt 3 "scrap should be called 3 times"

      let! _ = semaphore2.WaitAsync(100)

      let! jobState = context.JobMap.GetItem jobId

      Expect.equal jobState (Some expected) "job state is not expected"

     }
     |> Async.AwaitTask)
