namespace JobManager

open ScrapperModels
open ScrapperModels.ScrapperDispatcher
open ScrapperModels.JobManager
open Microsoft.Extensions.Logging
open System.Threading.Tasks

type Env =
  { Logger: ILogger
    ActorId: JobManagerId
    StateStore: ActorStore<State>
    ConfigStore: ActorStore<Config>
    CreateScrapperDispatcherActor: JobId -> IScrapperDispatcherActor
    GetEthBlocksCount: string -> Task<uint> }
