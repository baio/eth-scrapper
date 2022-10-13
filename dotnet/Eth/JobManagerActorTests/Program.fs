//module Program = let [<EntryPoint>] main _ = 0
open Expecto

[<EntryPoint>]
let main argv =
    Tests.runTestsInAssembly defaultConfig argv
