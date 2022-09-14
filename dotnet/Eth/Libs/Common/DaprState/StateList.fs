namespace Common.DaprState

[<AutoOpen>]
module StateList =

  open State
  open System.Threading.Tasks

  let getStateList<'a> env id =
    task {
      let! result = getStateAsync<'a list> env id

      return
        match result with
        | Some list -> list
        | None -> []
    }

  let getStateListHead<'a> env id fn =
    task {
      let! result = getStateAsync<'a list> env id

      return
        match result with
        | Some list -> list |> List.filter fn |> List.tryHead
        | None -> None
    }

  let insertStateList<'a> env id fn (entity: 'a) =

    task {
      let! result =
        tryUpdateOrCreateStateAsync env id (function
          | Some data ->
            let head = data |> List.tryFind fn

            match head with
            | Some head -> head |> Error
            | None -> entity :: data |> Ok
          | None -> Ok [ entity ])

      return result |> Result.map (fun _ -> entity)
    }


  let private toOptList =
    function
    | [] -> None
    | _ as x -> Some x

  let deleteStateList<'a when 'a: equality> env id (fr: 'a -> bool) =
    task {
      let! result = tryUpdateStateAsync' env id (List.filter (fr >> not) >> Some)

      match result with
      | Some (cur, orig) ->
        let res = List.except cur orig
        return res |> List.tryHead
      | None -> return None
    }

  let deleteStateListAll env id = deleteStateAsync env id

  let updateStateList<'a> env id (fr: 'a -> bool) mp =
    task {
      let! result =
        tryUpdateStateAsync'
          env
          id
          (List.map (fun x -> if fr x then mp x else x)
           >> toOptList)

      match result with
      | Some (cur, _) -> return cur |> List.filter (fr) |> List.tryHead
      | None -> return None
    }

  let stateListRepo<'a when 'a: equality> env =
    {| Insert = insertStateList<'a> env
       GetAll = getStateList<'a> env
       GetHead = getStateListHead<'a> env
       Delete = deleteStateList<'a> env
       Update = updateStateList<'a> env
       DeleteAll = deleteStateListAll env |}
