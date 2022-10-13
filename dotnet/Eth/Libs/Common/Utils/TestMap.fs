namespace Common.Utils.Test

[<AutoOpen>]
module Map =
  open System.Threading.Tasks

  let createMapHelper<'a, 'b when 'a: comparison> () =
    let mutable map = Map.empty

    {| AddItem =
        fun (key: 'a) (item: 'b) ->
          map <- Map.add key item map
          () |> Task.FromResult :> Task
       AddIfNotExist =
        fun (key: 'a) (item: 'b) ->
          let f = map.ContainsKey key

          match f with
          | false -> map <- Map.add key item map
          | true -> ()

          () |> Ok |> Task.FromResult :> Task
       RemoveItem =
        fun (key: 'a) ->
          map <- Map.remove key map
          true |> Task.FromResult
       GetItem = fun (key: 'a) -> map |> Map.tryFind key |> Task.FromResult
       GetAllItems =
        fun () ->
          map
          |> Map.toList
          |> List.map snd
          |> Ok
          |> Task.FromResult |}
