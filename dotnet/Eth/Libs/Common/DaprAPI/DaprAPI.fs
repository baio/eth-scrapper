namespace Common.DaprAPI

open Dapr.Actors.Client


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
  open Dapr.Abstracts
  open Dapr.Decorators

  let private getStateEnv (builder: WebApplicationBuilder) (serviceProvider: IServiceProvider) =

    let stateManager = serviceProvider.GetService<IStateManager>()

    let loggerFactory = serviceProvider.GetService<ILoggerFactory>()
    let logger = loggerFactory.CreateLogger()

    let storeName =
      builder
        .Configuration
        .GetSection("Dapr")
        .GetValue<string>("StoreName")

    { Logger = logger
      StoreName = storeName
      StateManager = stateManager }


  let createDaprAPI2 (mapper: ResultMapper option) (initServices: IServiceCollection -> unit) (args: string []) =

    let builder = WebApplication.CreateBuilder(args)

    DaprLogging.createSerilogLogger builder.Configuration builder.Host |> ignore

    let services = builder.Services

    let converter =
      JsonFSharpConverter(
        JsonUnionEncoding.InternalTag
        ||| JsonUnionEncoding.NamedFields
        ||| JsonUnionEncoding.UnwrapOption
        ||| JsonUnionEncoding.UnwrapSingleCaseUnions,
        allowNullFields = true,
        unionTagName = JsonUnionTagName "kind"
      )

    services
      .AddControllers(fun opts -> opts.Filters.Add(RepoResultFilter(mapper)))
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

    services.AddScoped<IActorFactory>(Func<_, _>(fun _ -> ActorFactory(ActorProxy.DefaultProxyFactory)))
    |> ignore

    services.AddScoped<IStateManager>(Func<_, _>(fun x -> StateManager(x.GetService<DaprClient>())))
    |> ignore

    services.AddScoped<StateEnv>(Func<_, _>(getStateEnv builder))
    |> ignore

    initServices services

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

    let port =
      if args.Length > 0 then
        args[0]
      else
        System.Environment.GetEnvironmentVariable("PORT")

    let url = $"http://*:{port}"

    app.Run(url)

    0


  let createDaprAPI = createDaprAPI2 None (fun _ -> ())
