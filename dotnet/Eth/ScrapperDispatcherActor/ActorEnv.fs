namespace ScrapperDispatcherActor

open ScrapperModels.ScrapperDispatcherActor
open System.Threading.Tasks
open Microsoft.Extensions.Logging
open Dapr.Actors

type ActorEnv =
  { SetState: State -> Task
    GetState: unit -> Task<State option>
    Logger: ILogger }
