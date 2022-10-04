namespace Common.Logger

[<RequireQualifiedAccess>]
module ConsoleLogger =
  open Microsoft.Extensions.Logging

  let createConsoleLogger logLevel name =
    LoggerFactory
      .Create(fun builder ->
        builder.SetMinimumLevel(logLevel).AddConsole()
        |> ignore)
      .CreateLogger(name)
