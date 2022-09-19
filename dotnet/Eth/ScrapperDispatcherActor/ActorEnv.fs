namespace ScrapperDispatcherActor

open ScrapperModels
open System.Threading.Tasks
open Microsoft.Extensions.Logging
open Dapr.Actors

type ActorEnv = {
  SetState: State -> Task
  GetState: unit -> Task<State option>
  Logger: ILogger
  ActorId: ActorId
}

