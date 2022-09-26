namespace ScrapperDispatcherActor

[<AutoOpen>]
module internal Schedule =

  open Dapr.Actors
  open System.Threading.Tasks
  open ScrapperModels.ScrapperDispatcher
  open Microsoft.Extensions.Logging
  open Common.DaprActor

  type ScheduleEnv = float -> Task<unit>

  let schedule ((env, scheduleEnv): ActorEnv * ScheduleEnv) =
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
                Date = epoch () }

          do! env.SetState updatedState

          do! scheduleEnv dueTime

          return state |> Ok
        else
          let error = "Can't schedule, wrong state {@state}"
          logger.LogDebug("Can't schedule, wrong state {@state}", state)
          return (state, error) |> StateConflict |> Error
      | None ->
        logger.LogDebug("Can't schedule, wrong state {@state}", state)
        return StateNotFound |> Error
    }
