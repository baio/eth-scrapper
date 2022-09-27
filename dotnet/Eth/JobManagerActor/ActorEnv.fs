namespace JobManager

open ScrapperModels.JobManager
open Microsoft.Extensions.Logging
open System.Threading.Tasks

type ActorEnv =
  { Logger: ILogger
    SetState: State -> Task
    GetState: unit -> Task<State option> }
