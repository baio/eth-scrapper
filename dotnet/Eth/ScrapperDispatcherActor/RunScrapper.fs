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

    task {

      use scope =
        logger.BeginScope("runScrapper {@data} {@state}", scrapperRequest, state)

      logger.LogDebug("Run scrapper")

      let state: State =
        { state with
            Status = Status.Continue
            Request = scrapperRequest
            Date = env.Date() |> toEpoch }

      do! env.SetState state

      let actor = env.CreateScrapperActor(env.ActorId)

      let! result = actor.Scrap scrapperRequest |> Task.wrapException

      logger.LogDebug("Run scrapper result {@result}", result)

      match result with
      | Ok _ -> return state |> Ok
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

  let runScrapperStart
    (env: Env)
    ((parentId, targetIsLatest): JobManagerId option * bool)
    (scrapperRequest: ScrapperRequest)
    =
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

    task { return! runScrapper env scrapperRequest state }
