module ParallelStartFinishTests

open Expecto
open ScrapperModels
open Common.Utils.Task
open Common.Utils
open ScrapperTestContext
open System.Threading
open System.Threading.Tasks

//[<Tests>]
let tests =

  let semaphore = new SemaphoreSlim(0, 2)
  let semaphore2 = new SemaphoreSlim(0, 1)

  let onScrap: OnScrap =
    fun request ->
      
      task {
        semaphore.Release() |> ignore
        let result: ScrapperModels.ScrapperResult =
          { Data = EmptyResult
            BlockRange = request.BlockRange }
          |> Error

        return result
      }

  let date = System.DateTime.UtcNow

  let env =
    { EthBlocksCount = 1000u
      MaxEthItemsInResponse = 100u
      OnScrap = onScrap
      MailboxHooks = None, None
      OnReportJobStateChanged = releaseOnSuccess semaphore2
      Date = fun () -> date }

  let context = Context env

  let expected: JobManager.State =
    { Status = JobManager.Status.Success
      LatestUpdateDate = date |> toEpoch |> Some
      Jobs =
        [ (JobId "1_s0",
           Ok(
             { Status = ScrapperDispatcher.Status.Finish
               Request =
                 { EthProviderUrl = "test"
                   ContractAddress = ""
                   Abi = ""
                   BlockRange = { From = 0u; To = 500u } }
               Date = date |> toEpoch
               FinishDate = date |> toEpoch |> Some
               ItemsPerBlock = []
               Target =
                 { ToLatest = false
                   Range = { From = 0u; To = 500u } }
               ParentId = Some(JobManagerId "1") }: ScrapperDispatcher.State
           ))
          (JobId "1_s1",
           Ok(
             { Status = ScrapperDispatcher.Status.Finish
               Request =
                 { EthProviderUrl = "test"
                   ContractAddress = ""
                   Abi = ""
                   BlockRange = { From = 500u; To = 1000u } }
               Date = date |> toEpoch
               FinishDate = date |> toEpoch |> Some
               ItemsPerBlock = []
               Target =
                 { ToLatest = true
                   Range = { From = 500u; To = 1000u } }
               ParentId = Some(JobManagerId "1") }: ScrapperDispatcher.State
           )) ]
        |> Map.ofList }

  testCaseAsync
    "parallel start finish"
    (task {

      let jobManagerId = JobManagerId "1"
      let jobManager = context.createJobManager jobManagerId

      let startData: JobManager.StartData =
        { EthProviderUrl = "test"
          ContractAddress = ""
          Abi = "" }

      let! _ = jobManager.SetJobsCount(2u)
      let! _ = jobManager.Start(startData)

      do! semaphore.WaitAsync()
      do! semaphore.WaitAsync()
      let! _ = semaphore2.WaitAsync()

      let! jobManangerState = context.JobManagerMap.GetItem jobManagerId

      Expect.equal jobManangerState (Some expected) "job mananger state is not expected"
      ()
     }
     |> Async.AwaitTask)
