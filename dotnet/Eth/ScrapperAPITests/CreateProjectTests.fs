module CreateProjectTests

open Expecto
open ScrapperModels
open Common.Utils.Task
open Common.Utils
open ScrapperTestContext
open ScrapperAPI.Services
open Common.DaprState
open Scrapper.Repo.PeojectsRepo
open Scrapper.Repo
open ScrapperAPI.Services.JobManagerService
open ScrapperModels.JobManager

[<FTests>]
let tests =

  let onScrap: OnScrap =
    fun request ->
      { Data = EmptyResult
        BlockRange = request.BlockRange }
      |> Error: ScrapperModels.ScrapperResult

  let now = System.DateTime.UtcNow

  let contextEnv =
    { EthBlocksCount = 1000u
      MaxEthItemsInResponse = 100u
      OnScrap = onScrap
      Date = fun () -> now }

  let context = Context contextEnv

  let stateEnv: StateEnv =
    { Logger = Common.Logger.SerilogLogger.createDefault "scrapper-api-tests"
      StateManager = Dapr.Decorators.InMemory.StateManager()
      StoreName = "scrapper-api-tests" }

  let repoEnv: RepoEnv =
    { StateEnv = stateEnv
      Now = fun () -> now }

  let projectEntry: ProjectEntity =
    { Id = "prefix_xxx"
      Name = "name"
      Prefix = "prefix"
      Address = "xxx"
      Abi = "xxx"
      EthProviderUrl = "xxx" }

  let version = { Id = "v1"; Created = now }

  let versionWithState: VersionWithState =
    { Version = version
      State =
        Some(
          { Status = Status.Initial
            Jobs = Map.empty
            LatestUpdateDate = None
            AvailableJobsCount = 1u }
        ) }

  let projectWithVersions: ProjectWithVersions =
    { Project = projectEntry
      Versions = [ { Id = "v1"; Created = now } ] }

  let projectWithVersionsAndState: ProjectWithVresionsAndState =
    { Project = projectEntry
      Versions = [ versionWithState ] }

  testSequenced
  <| testList
       "create project"
       [ testCase "create project" (fun _ ->
           let expected = projectWithVersions |> Ok

           task {
             let projectEntry: CreateProjectEntity =
               { Name = "name"
                 Prefix = "prefix"
                 Address = "xxx"
                 Abi = "xxx"
                 EthProviderUrl = "xxx"
                 VersionId = "v1" }

             let! actual = JobManagerService.createProject repoEnv projectEntry

             Expect.equal actual expected "create project result is not expected"
             ()
           }
           |> runSynchronously)

         testCase "get all projects" (fun _ ->
           let expected = [ projectWithVersionsAndState ] |> Ok

           task {
             let! actual = JobManagerService.getProjectVersionsWithState (repoEnv, context.createJobManager)
             Expect.equal actual expected "getProjectVersionsWithState is not expected"
             ()
           }
           |> runSynchronously) ]
