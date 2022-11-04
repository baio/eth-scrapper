namespace ScrapperAPI

open Dapr.Abstracts
open Dapr.Client

module Program =
  open Scrapper.Repo
  open Common.DaprState
  open Microsoft.Extensions.DependencyInjection
  open ScrapperAPI.Services
  open Dapr.Actors.Runtime
  open ScrapperModels.JobManager
  open ScrapperModels

  let private initServices (services: IServiceCollection) =
    services.AddScoped<RepoEnv> (fun x ->
      let stateEnv = x.GetService<StateEnv>()

      { StateEnv = stateEnv
        Now = fun () -> System.DateTime.Now })
    |> ignore

    services.AddScoped<JobManagerService.JobManagerActorFactory>(
      System.Func<_, _> (fun x ->
        let actorFactory = x.GetService<IActorFactory>()
        fun (JobManagerId x) -> actorFactory.CreateActor<IJobManagerActor>(x))
    )
    |> ignore

  [<EntryPoint>]
  let main args =
    Common.DaprAPI.DaprAPI.createDaprAPI2 (Some ResultMapper.mapResult) initServices args
