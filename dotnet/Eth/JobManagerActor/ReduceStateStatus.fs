namespace JobManager

[<RequireQualifiedAccess>]
module ReduceStateStatus =

  open ScrapperModels.JobManager

  let private isAllSuccess =
    List.forall (function
      | Ok (job: Job) -> job.Status = ScrapperModels.ScrapperDispatcher.Status.Finish
      | Error _ -> false)

  let private isAllFailed =
    List.forall (function
      | Ok _ -> false
      | Error _ -> true)

  let private isSomeFailed =
    List.tryFind (function
      | Ok _ -> false
      | Error _ -> true)
    >> Option.isSome

  let recalcStatus (jobs) =
    let list = jobs |> Map.toList |> List.map snd

    if (isAllFailed list) then
      Status.Failure
    else if (isSomeFailed list) then
      Status.PartialFailure
    else if (isAllSuccess list) then
      Status.Success
    else
      Status.Continue


  /// Recalc state status from Jobs statuses
  let reduce (state: State) =
    let jobs = state.Jobs
    let status = recalcStatus jobs

    { state with Status = status }
