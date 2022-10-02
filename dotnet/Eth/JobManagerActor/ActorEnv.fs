namespace JobManager

open ScrapperModels
open ScrapperModels.ScrapperDispatcher
open ScrapperModels.JobManager
open Microsoft.Extensions.Logging
open System.Threading.Tasks


type Env = {
    Logger: ILogger
    ActorId: JobManagerId
    SetState: State -> Task
    SetStateIfNotExist: State -> Task
    GetState: unit -> Task<State option> 
    CreateScrapperDispatcherActor: JobId -> IScrapperDispatcherActor
}
