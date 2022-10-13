namespace Common.Logger

[<RequireQualifiedAccess>]
module SerilogLogger =

  open Microsoft.Extensions.Configuration
  open Serilog
  open Serilog.Enrichers.Span
  open Serilog.Extensions.Logging
  open Destructurama


  let createSerilog (configuration: IConfiguration) =

    let loggerConfig =
      LoggerConfiguration()                
        .ReadFrom.Configuration(configuration)        
        .Enrich.FromLogContext()
        .Enrich.WithSpan()
        .Destructure.UsingAttributes()

    let logger = loggerConfig.CreateLogger()
    Log.Logger <- logger

    logger

  let create configuration (name: string) =
    let serilogLogger = createSerilog configuration
    let factory = new SerilogLoggerFactory(serilogLogger)
    let logger = factory.CreateLogger(name)
    logger

  let createDefault =
    let config = DefaultConfiguration.create ()
    create config
