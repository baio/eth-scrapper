namespace ScrapperDispatcherActor

[<AutoOpen>]
module internal RunScrapper =
  open Dapr.Actors
  open System.Threading.Tasks
  open ScrapperModels
  open Microsoft.Extensions.Logging
  open Common.DaprActor

  type RunScrapperEnv =
    { InvokeActor: ScrapperRequest -> Task<Result<unit, exn>>
      Logger: ILogger
      SetState: State -> Task }

  let runScrapper
    ({ InvokeActor = invokeActor
       Logger = logger
       SetState = setState }: RunScrapperEnv)
    (scrapperRequest: ScrapperRequest)
    (state: State)
    =

    logger.LogDebug("Run scrapper with {@data} {@state}", scrapperRequest, state)

    task {
      let! result = invokeActor scrapperRequest

      logger.LogDebug("Run scrapper result {@result}", result)

      match result with
      | Ok _ ->

        let state: State =
          { state with
              Status = Status.Continue
              Request = scrapperRequest
              Date = epoch () }

        do! setState state

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
              Date = epoch () }

        do! setState state

        return state |> ActorFailure |> Error
    }

  let runScrapperStart
    (env: RunScrapperEnv)
    ((parentId, targetIsLatest): string option * bool)
    (scrapperRequest: ScrapperRequest)
    =
    let state: State =
      { Status = Status.Continue
        Request = scrapperRequest
        Date = epoch ()
        FinishDate = None
        ItemsPerBlock = []
        Target =
          { ToLatest = targetIsLatest
            Range = scrapperRequest.BlockRange }
        ParentId = parentId }

    runScrapper env scrapperRequest state
