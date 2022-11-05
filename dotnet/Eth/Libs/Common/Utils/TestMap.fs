namespace Common.Utils.Test

[<AutoOpen>]
module Map =
  open System.Threading.Tasks

  let createMapHelper<'a, 'b when 'a: comparison> () =
    let mutable map = Map.empty

    {| AddItem =
        fun (key: 'a) (item: 'b) ->
          Task.Run (fun () ->
            map <- Map.add key item map
            ())
       AddIfNotExist =
        fun (key: 'a) (item: 'b) ->
          Task.Run (fun () ->
            let f = map.ContainsKey key

            match f with
            | false -> map <- Map.add key item map
            | true -> ()

            () |> Ok)
          :> Task
       RemoveItem =
        fun (key: 'a) ->
          Task.Run (fun () ->
            map <- Map.remove key map
            true)
       GetItem = fun (key: 'a) -> Task.Run(fun () -> map |> Map.tryFind key)
       GetAllItems = fun () -> Task.Run(fun () -> map |> Map.toList |> List.map snd |> Ok) |}
