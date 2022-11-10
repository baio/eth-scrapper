namespace Common.Utils.Test

[<AutoOpen>]
module Map =
  open System.Threading.Tasks

  let createMapHelper<'a, 'b when 'a: comparison> () =
    let mutable map = Map.empty

    {| AddItem =
        fun (key: 'a) (item: 'b) ->
          task {
            map <- Map.add key item map
            printfn "STATE SET !!!"
          }
          :> Task
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
          task {
            map <- Map.remove key map
            return true
          }
       GetItem = fun (key: 'a) -> task { return map |> Map.tryFind key }
       GetAllItems = fun () -> task { return map |> Map.toList |> List.map snd |> Ok } |}
