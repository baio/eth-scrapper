namespace Common.DaprActor

[<AutoOpen>]
module DaprLogging =

  open Microsoft.Extensions.Configuration
  open Serilog
  open Serilog.Enrichers.Span
  open Microsoft.Extensions.Hosting

  let createSerilogLogger (configuration: IConfiguration) (webHostBuilder: IHostBuilder) =

    let mutable logger =
      LoggerConfiguration()
        .ReadFrom.Configuration(configuration)
        .Enrich.FromLogContext()
        .Enrich.WithSpan()

    Log.Logger <- logger.CreateLogger()
    webHostBuilder.UseSerilog()
