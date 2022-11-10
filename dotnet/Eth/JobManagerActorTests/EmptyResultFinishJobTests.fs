module LimitExceedsContinueJobTests

open Expecto
open ScrapperModels
open Common.Utils
open ScrapperTestContext
open System.Threading
open System.Threading.Tasks

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

  let onAfter: OnAfter =
    fun (actorName, methodName) _ ->
      task {
        match (actorName, methodName) with
        | "JobActor", "Continue" -> semaphore.Release() |> ignore
        | _ -> ()

        return ()
      }

  let env =
    { EthBlocksCount = ethBlocksCount
      MaxEthItemsInResponse = maxEthItemsInResponse
      OnScrap = onScrap
      OnReportJobStateChanged = None
      MailboxHooks = None, (Some onAfter)
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
    "job: empty result finish job"
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

      let! actual = context.JobMap.GetItem jobId

      Expect.equal actual (Some expected) "job state is not expected"

     }
     |> Async.AwaitTask)
