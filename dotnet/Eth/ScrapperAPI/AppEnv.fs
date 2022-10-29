namespace ScrapperAPI

open Common.DaprState
open ScrapperModels
open ScrapperModels.JobManager

type JobManagerActorFactory = JobManagerId -> IJobManagerActor

type AppEnv = {
  StoreEnv: DaprStoreEnv
  JobManagerActorFactory: JobManagerActorFactory
}

