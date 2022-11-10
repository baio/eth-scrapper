module StartPauseTests

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

  let semaphore = new SemaphoreSlim(0, 1)
  let semaphore2 = new SemaphoreSlim(0, 1)

  let onScrap: OnScrap =
    fun request ->
      task {
        do! semaphore.WaitAsync()

        return
          match scrapCnt with
          | 0 ->
            scrapCnt <- scrapCnt + 1

            { Data = LimitExceeded
              BlockRange = request.BlockRange }
            |> Error: ScrapperModels.ScrapperResult
          | _ -> failwith "not expected"
      }


  let date = System.DateTime.UtcNow

  let env =
    { EthBlocksCount = ethBlocksCount
      MaxEthItemsInResponse = maxEthItemsInResponse
      OnScrap = onScrap
      OnReportJobStateChanged = releaseOnStatus JobManager.Status.Pause semaphore2
      Date = fun () -> date }

  let context = Context env

  let expected: JobManager.State =
    { Status = JobManager.Status.Pause
      LatestUpdateDate = date |> toEpoch |> Some
      Jobs =
        [ (JobId "1_s0",
           Ok(
             { Status = ScrapperDispatcher.Status.Pause
               Request =
                 { EthProviderUrl = ""
                   ContractAddress = ""
                   Abi = ""
                   BlockRange = { From = 0u; To = 100u } }
               Date = date |> toEpoch
               FinishDate = None
               ItemsPerBlock = []
               Target =
                 { ToLatest = true
                   Range = { From = 0u; To = 100u } }
               ParentId = Some(JobManagerId "1") }: ScrapperDispatcher.State
           )) ]
        |> Map.ofList }

  testCaseAsync
    "when started and then paused, state should be correct"
    (task {

      let jobId = JobId "1_s0"
      let jobManagerId = JobManagerId "1"
      let jobManager = context.createJobManager jobManagerId

      let startData: JobManager.StartData =
        { EthProviderUrl = ""
          ContractAddress = ""
          Abi = "" }

      let! _ = jobManager.Start(startData)

      let! _ = jobManager.Pause()

      semaphore.Release() |> ignore

      do! Task.Delay 100

      Expect.equal scrapCnt 1 "scrap calls should be 1"

      let! _ = semaphore2.WaitAsync 500

      do! Task.Delay 100

      let! jobState = context.JobMap.GetItem jobId

      let job = Map.tryFind jobId expected.Jobs

      Expect.equal (jobState |> Option.map Ok) job "job state is not expected"

      let! jobManangerState = context.JobManagerMap.GetItem jobManagerId

      Expect.equal jobManangerState (Some expected) "job mananger state is not expected"

     }
     |> Async.AwaitTask)
