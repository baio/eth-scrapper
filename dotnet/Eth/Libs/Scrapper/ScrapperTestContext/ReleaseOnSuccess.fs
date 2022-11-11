namespace ScrapperTestContext

[<AutoOpen>]
module ReleaseOnSuccess =
  open ScrapperModels.JobManager
  open System.Threading

  let releaseOnStatus status (semaphore: SemaphoreSlim) : ReportJobStateChanged option =
    Some (fun (_, st) ->
      if st.Status = status then
        semaphore.Release() |> ignore)

  let releaseOnSuccess x = x |> releaseOnStatus Status.Success


  let jobManagerReleaseOnAfter (status: Status) (semaphore: SemaphoreSlim) : OnAfter =
    fun (actorName, methodName) (result, _, _) ->
      task {
        match (actorName, methodName) with
        | "JobManagerActor", "ReportJobState" ->
          let result = result :?> Result<State, Error>

          match result with
          | Ok state when state.Status = status -> semaphore.Release() |> ignore
          | _ -> ()

        | _ -> ()

        return ()
      }

  let jobManagerSuccessReleaseOnAfter x =
    x |> jobManagerReleaseOnAfter Status.Success
