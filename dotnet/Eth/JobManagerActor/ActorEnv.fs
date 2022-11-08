namespace JobManager

open ScrapperModels
open ScrapperModels.ScrapperDispatcher
open ScrapperModels.JobManager
open Microsoft.Extensions.Logging
open System.Threading.Tasks

type ActorStore<'a> =
  { Set: 'a -> Task
    Get: unit -> Task<'a option>
    Remove: unit -> Task<bool> }

type Env =
  { Logger: ILogger
    ActorId: JobManagerId
    StateStore: ActorStore<State>
    ConfigStore: ActorStore<Config>
    CreateScrapperDispatcherActor: JobId -> IScrapperDispatcherActor
    GetEthBlocksCount: string -> Task<uint> }
