namespace ScrapperDispatcherActor

[<AutoOpen>]
module internal Schedule =

  open Dapr.Actors
  open System.Threading.Tasks
  open ScrapperModels.ScrapperDispatcher
  open Microsoft.Extensions.Logging
  open Common.Utils

  type ScheduleEnv = float -> Task<unit>

  let schedule (env: Env) =
    let dueTime = 60.
    let logger = env.Logger
    logger.LogInformation("Try schedule {dueTime}", dueTime)

    task {
      let! state = env.GetState()

      match state with
      | Some state ->
        if state.Status = Status.Finish then
          logger.LogInformation("Run schedule with {@state}", state)

          let updatedState =
            { state with
                Status = Status.Schedule
                Date = env.Date() |> toEpoch }

          do! env.SetState updatedState

          // TODO : !!!
          // do! scheduleEnv dueTime

          return state |> Ok
        else
          let error = "Can't schedule, wrong state {@state}"
          logger.LogDebug("Can't schedule, wrong state {@state}", state)
          return (state, error) |> StateConflict |> Error
      | None ->
        logger.LogDebug("Can't schedule, wrong state {@state}", state)
        return StateNotFound |> Error
    }
