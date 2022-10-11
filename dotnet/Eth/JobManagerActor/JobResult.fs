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
    |> ReduceStateStatus.reduce

  let updateStateWithJobResult
    (childData: CallChildActorData)
    (state: State)
    ((jobId, result): (JobId * Result<ScrapperDispatcherActorResult, exn>))
    =
    let job = mapJobResult childData result
    { state with Jobs = state.Jobs.Add(jobId, job) }
    |> ReduceStateStatus.reduce

  let updateStateWithJob
    (state: State)
    ((jobId, job): (JobId * Job))
    =
    let jobResult = Ok job
    let jobs = state.Jobs.Add(jobId, jobResult)
    let result = { state with Jobs = jobs } |> ReduceStateStatus.reduce
    result
