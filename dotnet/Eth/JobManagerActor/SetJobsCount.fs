namespace JobManager

[<AutoOpen>]
module SetJobsCount =

  open ScrapperModels.JobManager
  open Microsoft.Extensions.Logging
  open System.Threading.Tasks

  let setJobsCount (env: Env) (count: uint) : Task<Result<Config, string>> =

    let logger = env.Logger

    task {
      use scope = logger.BeginScope("setJobsCount {count}", count)

      logger.LogDebug("SetJobsCount {count}", count)

      if count = 0u && count > 10u then
        logger.LogDebug("Attemt to set wrong jobs count {count}", count)

        return
          "Wrong jobs count, must be in the range of [1, 10]"
          |> Error
      else
        let! config = getConfig env

        logger.LogDebug("Config {config}", config)

        let config = { config with AvailableJobsCount = count }
        do! env.ConfigStore.Set config
        logger.LogDebug("Jobs count updated {@config}", config)
        return config |> Ok

    }
