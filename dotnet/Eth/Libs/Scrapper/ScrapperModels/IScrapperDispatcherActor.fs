namespace ScrapperModels.ScrapperDispatcher

open ScrapperModels
open Dapr.Actors
open System.Threading.Tasks
open Common.DaprActor.ActorResult
open System.Runtime.Serialization

type TargetBlockRange = { ToLatest: bool; Range: BlockRange }

type StartData =
  { EthProviderUrl: string
    ContractAddress: string
    Abi: string
    Target: TargetBlockRange option
    ParentId: JobManagerId option }

type ContinueData =
  { EthProviderUrl: string
    ContractAddress: string
    Abi: string
    Result: ScrapperResult }


[<KnownType("KnownTypes")>]
type FailureStatus =
  | CallChildActorFailure of AppId
  | StoreFailure of string
  | ExternalServiceFailure of string
  static member KnownTypes() = knownTypes<FailureStatus> ()

type FailureData = { AppId: AppId; Status: FailureStatus }

type Failure =
  { Data: FailureData
    FailuresCount: uint }

[<RequireQualifiedAccess>]
[<KnownType("KnownTypes")>]
type Status =
  | Continue
  | Pause
  | Finish
  | Schedule
  | Failure of Failure
  static member KnownTypes() = knownTypes<Status> ()

type State =
  { Status: Status
    Request: ScrapperRequest
    Date: int64
    FinishDate: int64 option
    ItemsPerBlock: float list
    Target: TargetBlockRange
    ParentId: JobManagerId option }

[<KnownType("KnownTypes")>]
type ScrapperDispatcherActorError =
  | ActorFailure of State
  | StateConflict of State * string
  | StateNotFound
  static member KnownTypes() =
    knownTypes<ScrapperDispatcherActorError> ()

type ScrapperDispatcherActorResult = Result<State, ScrapperDispatcherActorError>

type ConfirmContinueData =
  { BlockRange: BlockRange
    Target: TargetBlockRange }

type IScrapperDispatcherActor =
  inherit IActor
  abstract Start: data: StartData -> Task<ScrapperDispatcherActorResult>
  abstract Continue: data: ContinueData -> Task<ScrapperDispatcherActorResult>
  abstract Pause: unit -> Task<ScrapperDispatcherActorResult>
  abstract Resume: unit -> Task<ScrapperDispatcherActorResult>
  abstract State: unit -> Task<State option>
  abstract Reset: unit -> Task<bool>
  //abstract Schedule: unit -> Task<ScrapperDispatcherActorResult>
  abstract Failure: data: FailureData -> Task<ScrapperDispatcherActorResult>
  abstract ConfirmContinue: data: ConfirmContinueData -> Task<ScrapperDispatcherActorResult>
