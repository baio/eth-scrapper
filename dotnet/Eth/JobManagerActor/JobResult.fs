namespace JobManager

module JobResult =
  open ScrapperModels
  open ScrapperModels.ScrapperDispatcher
  open ScrapperModels.JobManager

  let mapJobResult (methodName: ChildActorMethodName) (result: Result<ScrapperDispatcherActorResult, exn>) =
    match result with
    | Ok result ->
      match result with
      | Ok result -> result |> Ok
      | Error err ->
        (AppId.Dispatcher, methodName, err)
        |> JobError.JobError
        |> Error
    | Error e ->
      (AppId.Dispatcher, ChildActorMethodName.Start)
      |> JobError.CallChildActorFailure
      |> Error


  let updateStateWithJobsListResult
    (methodName: ChildActorMethodName)
    (state: State)
    (result: (JobId * Result<ScrapperDispatcherActorResult, exn>) list)
    =
    let jobs =
      result
      |> List.map (fun (k, v) -> k, mapJobResult methodName (v))
      |> Map.ofList

    { state with Jobs = jobs }

  let updateStateWithJobResult
    (methodName: ChildActorMethodName)
    (state: State)
    ((jobId, result): (JobId * Result<ScrapperDispatcherActorResult, exn>))
    =
    let job = mapJobResult methodName result
    { state with Jobs = state.Jobs.Add(jobId, job) }
