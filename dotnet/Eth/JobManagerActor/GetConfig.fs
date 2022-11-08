namespace JobManager

[<AutoOpen>]
module GetConfig =

  open ScrapperModels
  open ScrapperModels.JobManager
  open Microsoft.Extensions.Logging
  open System.Threading.Tasks
  open System

  let private defaultConfig: Config = { AvailableJobsCount = 1u }

  let getConfig (env: Env) : Task<Config> =
    task {
      let! config = env.ConfigStore.Get()

      return
        match config with
        | Some config -> config
        | None -> defaultConfig
    }
