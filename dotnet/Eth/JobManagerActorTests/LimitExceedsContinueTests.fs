module LimitExceedsContinueTests

open Expecto
open ScrapperModels
open Common.Utils.Task
open Common.Utils
open Context
open System.Threading.Tasks

let ethBlocksCount = 100u
let maxEthItemsInResponse = 50u

[<Tests>]
let tests =

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

  let expected: JobManager.State =
    { Status = JobManager.Status.Success
      AvailableJobsCount = 1u
      Jobs =
        [ (JobId "1_s0",
           Ok(
             { Status = ScrapperDispatcher.Status.Finish
               Request =
                 { EthProviderUrl = ""
                   ContractAddress = ""
                   Abi = ""
                   BlockRange = { From = 50u; To = 100u } }
               Date = date |> toEpoch
               FinishDate = date |> toEpoch |> Some
               ItemsPerBlock = [ 0.1f ]
               Target =
                 { ToLatest = true
                   Range = { From = 0u; To = 100u } }
               ParentId = Some(JobManagerId "1") }: ScrapperDispatcher.State
           )) ]
        |> Map.ofList }

  testCase "when scrapper returns limit exceeds and then success state should be success" (fun _ ->
    task {

      let jobManagerId = JobManagerId "1"
      let jobManager = context.createJobManager jobManagerId

      let startData: JobManager.StartData =
        { EthProviderUrl = ""
          ContractAddress = ""
          Abi = "" }

      let! _ = jobManager.Start(startData)

      do! context.wait (1000)

      let! jobManangerState = context.JobManagerMap.GetItem jobManagerId

      Expect.equal jobManangerState (Some expected) "job mananger state is not expected"

    }
    |> runSynchronously)
