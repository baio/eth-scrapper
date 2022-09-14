namespace StoreReaderActor

#nowarn "20"

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

module Program =
  let exitCode = 0

  [<EntryPoint>]
  let main args =

    let builder = WebApplication.CreateBuilder(args)

    let converter =
      JsonFSharpConverter(
        JsonUnionEncoding.ExternalTag
        ||| JsonUnionEncoding.UnwrapSingleCaseUnions,
        allowNullFields = true
      )

    builder.Services.AddActors (fun opts ->
      opts.JsonSerializerOptions.PropertyNameCaseInsensitive <- true
      opts.JsonSerializerOptions.PropertyNamingPolicy <- JsonNamingPolicy.CamelCase
      opts.JsonSerializerOptions.Converters.Add(converter)
      opts.Actors.RegisterActor<StoreReaderActor>())
   
    let app = builder.Build()

    app.UseRouting()

    app.UseEndpoints(fun endpoints -> endpoints.MapActorsHandlers() |> ignore)

    let port = System.Environment.GetEnvironmentVariable("Port")

    let url = $"http://*:{port}"

    app.Run(url)

    exitCode
