namespace ScrapperTestContext

[<AutoOpen>]
module ReleaseOnSuccess =
  open System.Threading

  let releaseOnStatus status (semaphore: SemaphoreSlim) : ReportJobStateChanged option =
    Some (fun (_, st) ->
      if st.Status = status then
        semaphore.Release() |> ignore)

  let releaseOnSuccess x =
    x
    |> releaseOnStatus ScrapperModels.JobManager.Status.Success
