namespace Common.Utils.Test

[<AutoOpen>]
module Logger =
  open Microsoft.Extensions.Logging

  let createConsoleLogger logLevel name =
    LoggerFactory
      .Create(fun builder ->
        builder.SetMinimumLevel(logLevel).AddConsole()
        |> ignore)
      .CreateLogger(name)
