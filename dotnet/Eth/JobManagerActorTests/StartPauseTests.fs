module StartPauseTests

open Expecto
open ScrapperModels
open Common.Utils.Task
open Common.Utils
open ScrapperTestContext
open System.Threading.Tasks
open System.Threading
open ScrapperModels.JobManager

let ethBlocksCount = 100u
let maxEthItemsInResponse = 50u

[<Tests>]
let tests =

  let mutable scrapCnt = 0

  let scrapSemaphore = new SemaphoreSlim(0, 1)
  let startSemaphore = new SemaphoreSlim(0, 1)
  let pauseSemaphore = new SemaphoreSlim(0, 1)

  let onScrap: OnScrap =
    fun request ->
      task {
        do! scrapSemaphore.WaitAsync()

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

  let mutable status = Status.Initial

  let onAfter: OnAfter =
    fun (actorName, methodName) (result, _, _) ->
      task {
        match (actorName, methodName) with
        | "JobManagerActor", "ReportJobState" ->
          let result = result :?> Result<State, Error>

          match result with
          | Ok state ->
            if state.Status = Status.Continue
               && status = Status.Initial then
              startSemaphore.Release() |> ignore

            if state.Status = Status.Pause then
              pauseSemaphore.Release() |> ignore

            status <- state.Status
          | _ -> ()

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

      // jobs still not initialized wait till continue emitted
      do! startSemaphore.WaitAsync()

      let! _ = jobManager.Pause()

      // job manager state is not changed !

      Expect.equal scrapCnt 0 "scrap calls should be 0"

      scrapSemaphore.Release() |> ignore

      let! _ = pauseSemaphore.WaitAsync()

      Expect.equal scrapCnt 1 "scrap calls should be 1"

      let! jobState = context.JobMap.GetItem jobId

      let job = Map.tryFind jobId expected.Jobs

      Expect.equal (jobState |> Option.map Ok) job "job state is not expected"

      let! jobManangerState = context.JobManagerMap.GetItem jobManagerId

      Expect.equal jobManangerState (Some expected) "job mananger state is not expected"

     }
     |> Async.AwaitTask)
