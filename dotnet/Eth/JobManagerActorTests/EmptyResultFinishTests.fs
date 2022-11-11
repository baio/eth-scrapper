module EmptyResultFinishTests

open Expecto
open ScrapperModels
open Common.Utils.Task
open Common.Utils
open ScrapperTestContext
open System.Threading
open ScrapperModels.JobManager

[<Tests>]
let tests =

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

  let onAfter: OnAfter = jobManagerSuccessReleaseOnAfter semaphore

  let env =
    { EthBlocksCount = 1000u
      MaxEthItemsInResponse = 100u
      OnScrap = onScrap
      MailboxHooks = None, (Some onAfter)
      OnReportJobStateChanged = releaseOnSuccess semaphore
      Date = fun () -> date }

  let context = Context env

  let expected: JobManager.State =
    { Status = JobManager.Status.Success
      LatestUpdateDate = date |> toEpoch |> Some
      Jobs =
        [ (JobId "2_s0",
           Ok(
             { Status = ScrapperDispatcher.Status.Finish
               Request =
                 { EthProviderUrl = "test"
                   ContractAddress = ""
                   Abi = ""
                   BlockRange = { From = 0u; To = 1000u } }
               Date = date |> toEpoch
               FinishDate = date |> toEpoch |> Some
               ItemsPerBlock = []
               Target =
                 { ToLatest = true
                   Range = { From = 0u; To = 1000u } }
               ParentId = Some(JobManagerId "2") }: ScrapperDispatcher.State
           )) ]
        |> Map.ofList }

  testCaseAsync
    "manager: when scrapper returns empty result (0 events) the job should finish"
    (task {

      let jobManagerId = JobManagerId "2"
      let jobManager = context.createJobManager jobManagerId

      let startData: JobManager.StartData =
        { EthProviderUrl = "test"
          ContractAddress = ""
          Abi = "" }

      let! _ = jobManager.Start(startData)

      do! semaphore.WaitAsync()

      let! jobManangerState = context.JobManagerMap.GetItem jobManagerId

      Expect.equal jobManangerState (Some expected) "job mananger state is not expected"
      ()
     }
     |> Async.AwaitTask)
