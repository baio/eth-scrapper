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

type Job = ScrapperDispatcherActorResult

type State =
  { Jobs: Map<JobId, Job>
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
