namespace ScrapperModels

[<AutoOpen>]
module internal Helpers =
  //https://diffluxum.wordpress.com/2011/06/23/f-discriminated-union-wcf-and-datacontract-attribute/
  open System.Reflection
  open Microsoft.FSharp.Reflection
  let knownTypes<'a> () =
    typeof<'a>.GetNestedTypes (BindingFlags.Public ||| BindingFlags.NonPublic)
    |> Array.filter FSharpType.IsUnion
