namespace Common.DaprActor

[<AutoOpen>]
module DaprActor =

  open System
  open System.Collections.Generic
  open System.IO
  open System.Linq
  open System.Threading.Tasks
  open Microsoft.AspNetCore
  open Microsoft.AspNetCore.Builder
  open Microsoft.AspNetCore.Hosting
  open Microsoft.AspNetCore.HttpsPolicy
  open Microsoft.Extensions.Configuration
  open Microsoft.Extensions.DependencyInjection
  open Microsoft.Extensions.Hosting
  open Microsoft.Extensions.Logging
  open System.Text.Json
  open System.Text.Json.Serialization
  open Dapr.Actors.Runtime


  type RegisterActors = ActorRuntimeOptions -> unit

  let createDaprActor (registerer: RegisterActors) (args: string array) =
    let builder = WebApplication.CreateBuilder(args)

    createSerilogLogger builder.Configuration builder.Host
    |> ignore

    let converter =
      JsonFSharpConverter(
        JsonUnionEncoding.InternalTag
        ||| JsonUnionEncoding.NamedFields
        ||| JsonUnionEncoding.UnwrapOption
        ||| JsonUnionEncoding.UnwrapSingleCaseUnions,
        allowNullFields = true,
        unionTagName = JsonUnionTagName "kind"
      )

    builder.Services.AddActors (fun opts ->
      opts.JsonSerializerOptions.PropertyNameCaseInsensitive <- true
      opts.JsonSerializerOptions.PropertyNamingPolicy <- JsonNamingPolicy.CamelCase
      opts.JsonSerializerOptions.Converters.Add(converter)
      registerer opts)

    //builder.Services.AddDaprSidekick(builder.Configuration) |> ignore

    let app = builder.Build()

    app.UseRouting() |> ignore

    app.UseEndpoints(fun endpoints -> endpoints.MapActorsHandlers() |> ignore)
    |> ignore

    let port =
      if args.Length > 0 then
        args[0]
      else
        builder.Configuration.GetValue<string>("PORT")

    let url = $"http://*:{port}"

    app.Run(url)

    0
