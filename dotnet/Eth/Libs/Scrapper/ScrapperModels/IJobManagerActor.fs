namespace ScrapperModels.JobManager

open Dapr.Actors
open System.Threading.Tasks
open ScrapperModels
open System.Runtime.Serialization
open ScrapperModels.ScrapperDispatcher

type RequestContinueData =
  { ActorId: JobId
    BlockRange: BlockRange
    Target: TargetBlockRange }

type Job = ScrapperDispatcher.State

type Status =
  | Initial
  | Continue
  | Success
  | PartialFailure
  | Failure
  | Pause

type StartData =
  { EthProviderUrl: string
    ContractAddress: string
    [<Destructurama.Attributed.NotLogged>]
    Abi: string }

type CallChildActorData =
  | Start of ScrapperDispatcher.StartData
  | ConfirmContinue of ConfirmContinueData

type JobError =
  | CallChildActorFailure of CallChildActorData
  | JobError of CallChildActorData * ScrapperDispatcherActorError

type JobResult = Result<Job, JobError>

type JobStateData = { ActorId: JobId; Job: Job }

type JobsMap = Map<JobId, JobResult>

type State =
  { Status: Status
    Jobs: JobsMap
    LatestUpdateDate: int64 option }

type Config = { AvailableJobsCount: uint }

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
  abstract SetJobsCount: count: uint -> Task<Result<Config, string>>
  abstract Config: unit -> Task<Config>
  abstract State: unit -> Task<State option>
  abstract Pause: unit -> Task<Result>
  abstract Resume: unit -> Task<Result>
  abstract Reset: unit -> Task<Result>
  abstract ReportJobState: data: JobStateData -> Task<Result>
