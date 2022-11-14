namespace ScrapperDispatcherActor

[<AutoOpen>]
module internal RequestContinue =
  open Dapr.Actors
  open System.Threading.Tasks
  open ScrapperModels.JobManager
  open ScrapperModels.ScrapperDispatcher
  open Microsoft.Extensions.Logging
  open Common.Utils
  open ScrapperModels

  let requestContinue (env: Env) (parentId: JobManagerId) (data: RequestContinueData) (state: State) =

    let logger = env.Logger

    task {

      use scope = logger.BeginScope("requestContinue {@data} {@state}", data, state)
      logger.LogDebug("Request continue")

      let state: State =
        { state with
            Status = Status.Continue
            Request = { state.Request with BlockRange = data.BlockRange }
            Target = data.Target
            Date = (env.Date() |> toEpoch) }

      do! env.StateStore.Set state

      let actor = env.CreateJobManagerActor(parentId)

      actor.RequestContinue data |> ignore

      return state |> Ok
    }
