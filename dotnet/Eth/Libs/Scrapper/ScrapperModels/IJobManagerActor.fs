namespace ScrapperModels.JobManager

open Dapr.Actors
open System.Threading.Tasks
open ScrapperModels
open System.Runtime.Serialization
open ScrapperModels.ScrapperDispatcher

type RequestContinueData =
  { ActorId: string
    BlockRange: BlockRange
    Target: TargetBlockRange }

type JobId = JobId of string

type Job = ScrapperDispatcher.State

type Status =
  | Initial
  | Continue
  | Success
  | PartialFailure
  | Failure

type ChildActorMethodName =
  | Start
  | ConfirmContinue

type JobError =
  | CallChildActorFailure of AppId * ChildActorMethodName
  | JobError of AppId * ChildActorMethodName * ScrapperDispatcherActorError

type JobResult = Result<Job, JobError>

type State =
  { Status: Status
    Jobs: Map<JobId, JobResult>
    AvailableJobsCount: uint }

type StartData =
  { EthProviderUrl: string
    ContractAddress: string
    Abi: string }


[<KnownType("KnownTypes")>]
type Error =
  | ActorFailure of State
  | StateConflict of State * string
  | StateNotFound
  | ValidationFailure of string
  static member KnownTypes() = knownTypes<Error> ()

type Result = Result<State, Error>

type IJobManagerActor =
  inherit IActor
  abstract Start: data: StartData -> Task<Result>
  abstract RequestContinue: data: RequestContinueData -> Task<Result>
  abstract SetJobsCount: count: uint -> Task<Result>
  abstract State: unit -> Task<State option>
  abstract Pause: unit -> Task<Result>
  abstract Resume: unit -> Task<Result>
  abstract Reset: unit -> Task<Result>
