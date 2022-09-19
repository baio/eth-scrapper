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

  type RunScrapperState =
    | Start of targetIsLatest: bool
    | Continue of State


  let runScrapper
    ({ InvokeActor = invokeActor
       Logger = logger
       SetState = setState }: RunScrapperEnv)
    (state: RunScrapperState)
    (scrapperRequest: ScrapperRequest)
    =

    logger.LogDebug("Run scrapper with {@data} {@state}", scrapperRequest, state)

    task {
      let! result = invokeActor scrapperRequest

      logger.LogDebug("Run scrapper result {@result}", result)

      let finishDate =
        match state with
        | Continue state -> state.FinishDate
        | Start _ -> None

      let latestSuccesses =
        match state with
        | Continue state -> state.ItemsPerBlock
        | Start _ -> []

      let target =
        match state with
        | Continue state -> state.Target
        | Start targetIsLatest ->
          { ToLatest = targetIsLatest
            Range = scrapperRequest.BlockRange }

      match result with
      | Ok _ ->
        let state: State =
          { Status = Status.Continue
            Request = scrapperRequest
            Date = epoch ()
            FinishDate = finishDate
            ItemsPerBlock = latestSuccesses
            Target = target }

        do! setState state

        return state |> Ok
      | Error _ ->
        let state: State =
          { Status =
              { Data =
                  { AppId = AppId.Dispatcher
                    Status = AppId.Scrapper |> CallChildActorFailure }
                RetriesCount = 0u }
              |> Status.Failure
            Request = scrapperRequest
            Date = epoch ()
            FinishDate = finishDate
            ItemsPerBlock = latestSuccesses
            Target = target }

        do! setState state

        return state |> ActorFailure |> Error
    }
