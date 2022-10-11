module EmptyResultFinishTests

open Expecto
open ScrapperModels
open Common.Utils.Task
open Common.Utils
open Context

[<Tests>]
let tests =

  let onScrap: OnScrap =
    fun request ->
      { Data = EmptyResult
        BlockRange = request.BlockRange }
      |> Error: ScrapperModels.ScrapperResult

  let date = System.DateTime.UtcNow

  let env =
    { EthBlocksCount = 1000u
      MaxEthItemsInResponse = 100u
      OnScrap = onScrap
      Date = fun () -> date }

  let context = Context env

  let expected: JobManager.State =
    { Status = JobManager.Status.Success
      AvailableJobsCount = 1u
      Jobs =
        [ (JobId "1_s0",
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
               ParentId = Some(JobManagerId "1") }: ScrapperDispatcher.State
           )) ]
        |> Map.ofList }

  testCase "manager: when scrapper returns empty result (0 events) the job should finish" (fun _ ->
    task {

      let jobManagerId = JobManagerId "1"
      let jobManager = context.createJobManager jobManagerId

      let startData: JobManager.StartData =
        { EthProviderUrl = "test"
          ContractAddress = ""
          Abi = "" }

      let! _ = jobManager.Start(startData)

      do! context.wait (500)

      let! jobManangerState = context.JobManagerMap.GetItem jobManagerId

      Expect.equal jobManangerState (Some expected) "job mananger state is not expected"
      ()
    }
    |> runSynchronously)
