namespace JobManager

module JobResult =
  open ScrapperModels
  open ScrapperModels.ScrapperDispatcher
  open ScrapperModels.JobManager

  let mapJobResult (childData: CallChildActorData) (result: Result<ScrapperDispatcherActorResult, exn>) =
    match result with
    | Ok result ->
      match result with
      | Ok result -> result |> Ok
      | Error err -> (childData, err) |> JobError |> Error
    | Error _ -> childData |> CallChildActorFailure |> Error


  let updateStateWithJobsListResult
    (state: State)
    (result: (JobId * Result<ScrapperDispatcherActorResult, exn> * CallChildActorData) list)
    =
    let jobs =
      result
      |> List.map (fun (k, v, d) -> k, mapJobResult d (v))
      |> Map.ofList

    { state with Jobs = jobs }

  let updateStateWithJobResult
    (childData: CallChildActorData)
    (state: State)
    ((jobId, result): (JobId * Result<ScrapperDispatcherActorResult, exn>))
    =
    let job = mapJobResult childData result
    { state with Jobs = state.Jobs.Add(jobId, job) }
