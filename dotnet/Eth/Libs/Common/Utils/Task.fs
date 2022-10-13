namespace Common.Utils

module Task =

  open System.Threading.Tasks

  let all tasks =
    async {
      let! result =
        tasks
        |> List.map (Async.AwaitTask)
        |> Async.Parallel

      return result |> List.ofArray
    }

  let wrapException<'a> (t: Task<'a>) =
    task {
      try
        let! result = t
        return result |> Ok
      with
      | _ as ex -> return ex |> Error
    }

  let runSynchronously x =
    x |> Async.AwaitTask |> Async.RunSynchronously
