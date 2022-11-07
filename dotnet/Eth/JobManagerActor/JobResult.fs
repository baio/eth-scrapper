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


  let private mergeMap (m1: Map<_, _>) (m2: Map<_, _>) =
    m1
    |> Map.fold
         (fun s k v ->
           match Map.tryFind k m2 with
           | Some v -> s
           | None -> Map.add k v s)
         m2

  /// update state only for error results  !
  let updateStateWithJobsListErrorResult
    (state: State)
    (result: (JobId * Result<ScrapperDispatcherActorResult, exn> * CallChildActorData) list)
    =
    let jobs =
      result
      |> List.choose (fun (k, v, d) ->
        match v with
        | Error _ -> Some(k, mapJobResult d (v))
        | _ -> None)

    match jobs with
    | [] -> None
    | _ ->
      let jobs = jobs |> Map.ofList |> mergeMap state.Jobs

      { state with Jobs = jobs }
      |> ReduceStateStatus.reduce
      |> Some

  let updateStateWithJobsListResult
    (state: State)
    (result: (JobId * Result<ScrapperDispatcherActorResult, exn> * CallChildActorData) list)
    =
    let jobs =
      result
      |> List.map (fun (k, v, d) -> (k, mapJobResult d (v)))

    let jobs = jobs |> Map.ofList |> mergeMap state.Jobs

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

  let updateStateWithJob (state: State) ((jobId, job): (JobId * Job)) =
    let jobResult = Ok job
    let jobs = state.Jobs.Add(jobId, jobResult)

    let result =
      { state with Jobs = jobs }
      |> ReduceStateStatus.reduce

    result
