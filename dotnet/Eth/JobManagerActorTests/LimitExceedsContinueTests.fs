module LimitExceedsContinueTests

open Expecto
open ScrapperModels
open Common.Utils.Task
open Common.Utils
open ScrapperTestContext
open System.Threading.Tasks
open System.Threading

let ethBlocksCount = 100u
let maxEthItemsInResponse = 50u

[<Tests>]
let tests =

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

  let onAfter = jobManagerSuccessReleaseOnAfter semaphore2

  let env =
    { EthBlocksCount = ethBlocksCount
      MaxEthItemsInResponse = maxEthItemsInResponse
      OnScrap = onScrap
      OnReportJobStateChanged = None
      MailboxHooks = None, (Some onAfter)
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
                 { EthProviderUrl = ""
                   ContractAddress = ""
                   Abi = ""
                   BlockRange = { From = 50u; To = 275u } }
               Date = date |> toEpoch
               FinishDate = date |> toEpoch |> Some
               ItemsPerBlock = [ 0.2 ]
               Target =
                 { ToLatest = true
                   Range = { From = 0u; To = 100u } }
               ParentId = Some(JobManagerId "1") }: ScrapperDispatcher.State
           )) ]
        |> Map.ofList }

  testCaseAsync
    "manager: when scrapper returns limit exceeds and then success state should be success"
    (task {

      let jobId = JobId "1_s0"
      let jobManagerId = JobManagerId "1"
      let jobManager = context.createJobManager jobManagerId

      let startData: JobManager.StartData =
        { EthProviderUrl = ""
          ContractAddress = ""
          Abi = "" }

      let! _ = jobManager.Start(startData)

      do! semaphore.WaitAsync()
      do! semaphore.WaitAsync()
      do! semaphore.WaitAsync()

      Expect.equal scrapCnt 3 "scrap calls should be 3"

      do! semaphore2.WaitAsync()

      let! jobState = context.JobStateMap.GetItem jobId

      let job = Map.tryFind jobId expected.Jobs

      Expect.equal (jobState |> Option.map Ok) job "job state is not expected"

      let! jobManangerState = context.JobManagerStateMap.GetItem jobManagerId

      Expect.equal jobManangerState (Some expected) "job mananger state is not expected"

     }
     |> Async.AwaitTask)
