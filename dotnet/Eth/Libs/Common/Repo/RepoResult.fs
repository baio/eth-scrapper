namespace Common.Repo

open System.Threading.Tasks

type RepoError =
  | NotFound
  | Frobidden
  | Conflict of obj
  | Unexpected of string

type RepoResult'<'a> = Result<'a, RepoError>
type RepoResult<'a> = Task<RepoResult'<'a>>

[<AutoOpen>]
module RepoResult =

  let noneTo'<'a> t (opt: 'a option) =
    match opt with
    | Some x -> x |> Ok
    | None -> t |> Error

  let noneToNotFound<'a> = noneTo'<'a> NotFound

  let errorToConflict<'a> (result: Result<'a, 'a>) =
    match result with
    | Ok x -> Ok x
    | Error x -> x |> box |> Conflict |> Error

  let taskMap fn t =
    task {
      let! x = t
      return fn x
    }
