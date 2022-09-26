namespace ScrapperDispatcherActor

open ScrapperModels.ScrapperDispatcher
open System.Threading.Tasks
open Microsoft.Extensions.Logging

type ActorEnv =
  { SetState: State -> Task
    GetState: unit -> Task<State option>
    Logger: ILogger }
