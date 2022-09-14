namespace Common.Utils

module Opt =

  let toNullable =
    function
    | Some x -> x |> box
    | None -> null
