namespace Common.Utils

module Task =

  let all tasks =
    async {
      let! result =
        tasks
        |> List.map (Async.AwaitTask)
        |> Async.Parallel

      return result |> List.ofArray
    }
