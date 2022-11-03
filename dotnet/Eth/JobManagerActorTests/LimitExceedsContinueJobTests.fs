module EmptyResultFinishJobTests

open Expecto
open ScrapperModels
open Common.Utils.Task
open Common.Utils
open ScrapperTestContext

[<Tests>]
let tests =
  let ethBlocksCount = 1000u
  let maxEthItemsInResponse = 100u

  let mutable scrapCnt = 0

  let onScrap: OnScrap =
    fun request ->
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

  let date = System.DateTime.UtcNow

  let env =
    { EthBlocksCount = ethBlocksCount
      MaxEthItemsInResponse = maxEthItemsInResponse
      OnScrap = onScrap
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

  testCase "job: when scrapper returns empty result (0 events) the job should finish" (fun _ ->
    task {

      let jobId = JobId "1"
      let job = context.createScrapperDispatcher jobId

      let startData: ScrapperDispatcher.StartData =
        { EthProviderUrl = "http://test"
          ContractAddress = ""
          Abi = ""
          Target = None
          ParentId = None }

      let! _ = job.Start(startData)

      do! context.wait (500)

      Expect.equal scrapCnt 3 "scrap should be called 3 times"

      let! jobState = context.JobMap.GetItem jobId

      Expect.equal jobState (Some expected) "job state is not expected"

    }
    |> runSynchronously)
