namespace JobManager


[<AutoOpen>]
module RequestContinue =

  open ScrapperModels
  open ScrapperModels.JobManager
  open System.Threading.Tasks

  type ScrapperDispatcherConfirmContiunue = string -> ScrapperDispatcher.ConfirmContinueData -> Task<Job>

  type RequestContinueEnv =
    { ScrapperDispatcherConfirmContiunue: ScrapperDispatcherConfirmContiunue }

  let requestContinue
    ((actorEnv, requestContinueEnv): ActorEnv * RequestContinueEnv)
    (data: RequestContinueData)
    : Task<Result> =
    task {

      let! state = actorEnv.GetState()

      match state with
      | Some state ->

        let confirmData: ScrapperDispatcher.ConfirmContinueData =
          { BlockRange = data.BlockRange
            Target = data.Target }

        let! result = requestContinueEnv.ScrapperDispatcherConfirmContiunue data.ActorId confirmData

        let jobId = JobId data.ActorId
        let jobs = state.Jobs |> Map.add jobId result
        let state = { state with Jobs = jobs }
        do! actorEnv.SetState state
        return state |> Ok
      | None -> return StateNotFound |> Error

    }
