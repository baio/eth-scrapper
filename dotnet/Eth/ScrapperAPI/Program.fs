namespace ScrapperAPI

module Program =
  open Scrapper.Repo
  open Common.DaprState
  open Microsoft.Extensions.DependencyInjection

  [<EntryPoint>]
  let main args =
    let services = Common.DaprAPI.DaprAPI.createDaprAPI args

    services.AddScoped<RepoEnv> (fun x ->
      let stateEnv = x.GetService<StateEnv>()

      { StateEnv = stateEnv
        Now = fun () -> System.DateTime.Now })
    |> ignore

    0
