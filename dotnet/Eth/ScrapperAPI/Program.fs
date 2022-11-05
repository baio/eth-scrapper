namespace ScrapperAPI

module Program =
  open Scrapper.Repo
  open Common.DaprState
  open Microsoft.Extensions.DependencyInjection
  open ScrapperAPI.Services
  open Dapr.Actors.Client
  open ScrapperModels.JobManager
  open ScrapperModels
  open Dapr.Abstracts
  open Dapr.Client
  open Dapr.Actors

  let private initServices (services: IServiceCollection) =
    services.AddScoped<RepoEnv> (fun x ->
      let stateEnv = x.GetService<StateEnv>()

      { StateEnv = stateEnv
        Now = fun () -> System.DateTime.Now })
    |> ignore

    services.AddScoped<JobManagerService.JobManagerActorFactory>(
      System.Func<_, _> (fun _ ->
        fun (JobManagerId actorId) -> ActorProxy.Create<IJobManagerActor>(ActorId(actorId), "job-manager"))
    )
    |> ignore

  [<EntryPoint>]
  let main args =
    Common.DaprAPI.DaprAPI.createDaprAPI2 (Some ResultMapper.mapResult) initServices args
