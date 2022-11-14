namespace ScrapperDispatcherActor

open ScrapperModels
open ScrapperModels.Scrapper
open ScrapperModels.JobManager
open ScrapperModels.ScrapperDispatcher
open System.Threading.Tasks
open Microsoft.Extensions.Logging

type Env =
  { MaxEthItemsInResponse: uint
    ActorId: JobId
    Date: unit -> System.DateTime
    StateStore: ActorStore<State>
    Logger: ILogger
    CreateJobManagerActor: JobManagerId -> IJobManagerActor
    CreateScrapperActor: JobId -> IScrapperActor
    GetEthBlocksCount: string -> Task<uint> }
