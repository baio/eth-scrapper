namespace ScrapperDispatcherActor

[<AutoOpen>]
module internal RunScrapper =
  open Dapr.Actors
  open System.Threading.Tasks
  open ScrapperModels.ScrapperDispatcher
  open ScrapperModels
  open Microsoft.Extensions.Logging
  open Common.Utils

  let runScrapper (env: Env) (scrapperRequest: ScrapperRequest) (state: State) =

    let logger = env.Logger

    logger.LogDebug("Run scrapper with {@data} {@state}", scrapperRequest, state)

    task {
      let actor = env.CreateScrapperActor(env.ActorId)

      let! result =
        actor.Scrap scrapperRequest
        |> Common.Utils.Task.wrapException

      logger.LogDebug("Run scrapper result {@result}", result)

      match result with
      | Ok _ ->

        let state: State =
          { state with
              Status = Status.Continue
              Request = scrapperRequest
              Date = env.Date() |> toEpoch }

        do! env.SetState state

        return state |> Ok
      | Error _ ->
        let state: State =
          { state with
              Status =
                { Data =
                    { AppId = AppId.Dispatcher
                      Status = AppId.Scrapper |> CallChildActorFailure }
                  FailuresCount = 0u }
                |> Status.Failure
              Request = scrapperRequest
              Date = env.Date() |> toEpoch }

        do! env.SetState state

        return state |> ActorFailure |> Error
    }

  let runScrapperStart (env: Env) ((parentId, targetIsLatest): JobManagerId * bool) (scrapperRequest: ScrapperRequest) =
    let state: State =
      { Status = Status.Continue
        Request = scrapperRequest
        Date = env.Date() |> toEpoch
        FinishDate = None
        ItemsPerBlock = []
        Target =
          { ToLatest = targetIsLatest
            Range = scrapperRequest.BlockRange }
        ParentId = parentId }

    runScrapper env scrapperRequest state
