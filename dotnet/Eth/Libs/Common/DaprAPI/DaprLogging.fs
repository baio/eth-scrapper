namespace Common.DaprAPI

[<AutoOpen>]
module DaprLogging =
  
  open Microsoft.Extensions.Hosting
  open Microsoft.Extensions.Configuration
  open Microsoft.AspNetCore.Hosting
  open System
  open Serilog
  open Serilog.Sinks.Elasticsearch
  open Serilog.Enrichers.Span

  let createSerilogLogger (configuration: IConfiguration) (webHostBuilder: IHostBuilder) =

    let mutable logger =
      LoggerConfiguration()
        .ReadFrom.Configuration(configuration)
        .Enrich.FromLogContext()
        .Enrich.WithSpan()

    Log.Logger <- logger.CreateLogger()
    webHostBuilder.UseSerilog()
