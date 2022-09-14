namespace Common.DaprAPI


[<AutoOpen>]
module DaprAPI =

  open System
  open Microsoft.AspNetCore.Builder
  open Microsoft.Extensions.Configuration
  open Microsoft.Extensions.DependencyInjection
  open Microsoft.Extensions.Logging
  open System.Text.Json
  open System.Text.Json.Serialization
  open Dapr.Client
  open Common.DaprState


  let private getDaprAppEnv (serviceProvider: IServiceProvider) =
    let loggerFactory = serviceProvider.GetService<ILoggerFactory>()
    let daprClient = serviceProvider.GetService<DaprClient>()
    let logger = loggerFactory.CreateLogger()
    { Logger = logger; Dapr = daprClient }


  let createDaprAPI (args: string []) =

    let builder = WebApplication.CreateBuilder(args)

    DaprLogging.createSerilogLogger builder.Configuration builder.Host
    |> ignore

    let services = builder.Services

    let converter =
      JsonFSharpConverter(
        JsonUnionEncoding.ExternalTag
        ||| JsonUnionEncoding.UnwrapSingleCaseUnions,
        allowNullFields = true
      )

    services
      .AddControllers(fun opts -> opts.Filters.Add(RepoResultFilter()))
      .AddJsonOptions(fun opts ->
        opts.JsonSerializerOptions.PropertyNameCaseInsensitive <- true
        opts.JsonSerializerOptions.PropertyNamingPolicy <- JsonNamingPolicy.CamelCase
        opts.JsonSerializerOptions.Converters.Add(converter))
    |> ignore


    services.AddDaprClient (fun builder ->
      let jsonOpts = JsonSerializerOptions()
      jsonOpts.PropertyNameCaseInsensitive <- true
      jsonOpts.PropertyNamingPolicy <- JsonNamingPolicy.CamelCase
      jsonOpts.Converters.Add(converter)

      builder.UseJsonSerializationOptions(jsonOpts)
      |> ignore)

    services.AddScoped<DaprAppEnv>(Func<_, _>(getDaprAppEnv))
    |> ignore

    services.AddScoped<DaprStoreEnv>(
      Func<_, _> (fun (serviceProvider: IServiceProvider) ->
        let app = getDaprAppEnv serviceProvider

        let storeName =
          builder
            .Configuration
            .GetSection("Dapr")
            .GetValue<string>("StoreName")

        { App = app; StoreName = storeName })
    )
    |> ignore

    let app = builder.Build()

    app.UseCors (fun x ->
      x
        .AllowAnyMethod()
        .AllowAnyHeader()
        .SetIsOriginAllowed(fun _ -> true)
        .AllowCredentials()
      |> ignore)
    |> ignore


    app.MapControllers() |> ignore

    let port = System.Environment.GetEnvironmentVariable("PORT")

    let url = $"http://*:{port}"

    app.Run(url)

    0
