namespace ScrapperDispatcherActor

open ScrapperModels
open ScrapperModels.Scrapper
open ScrapperModels.JobManager
open ScrapperModels.ScrapperDispatcher
open System.Threading.Tasks
open Microsoft.Extensions.Logging

type Env =
  { ActorId: JobId
    Date: unit -> System.DateTime
    SetState: State -> Task
    GetState: unit -> Task<State option>
    RemoveState: unit -> Task<bool>
    Logger: ILogger
    CreateJobManagerActor: JobManagerId -> IJobManagerActor
    CreateScrapperActor: JobId -> IScrapperActor }
