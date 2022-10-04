module EmptyResultFinishJobTests

open Expecto
open ScrapperModels
open Common.Utils.Task
open Common.Utils
open Context

[<FTests>]
let tests =
  let ethBlocksCount = 1000u
  let maxEthItemsInResponse = 100u

  let onScrap: OnScrap =
    fun request ->
      { Data = EmptyResult
        BlockRange = request.BlockRange }
      |> Error: ScrapperModels.ScrapperResult

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
          BlockRange = { From = 0u; To = ethBlocksCount } }
      Date = date |> toEpoch
      FinishDate = date |> toEpoch |> Some
      ItemsPerBlock = []
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

      do! context.wait (100)

      let! jobManangerState = context.JobMap.GetItem jobId

      Expect.equal jobManangerState (Some expected) "job state is not expected"

    }
    |> runSynchronously)
