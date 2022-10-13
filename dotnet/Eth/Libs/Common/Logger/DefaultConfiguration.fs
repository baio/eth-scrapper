namespace Common.Logger

[<RequireQualifiedAccessAttribute>]
module DefaultConfiguration =
  open Microsoft.Extensions.Configuration

  let create () =
    (new ConfigurationManager())
      .AddJsonFile("appsettings.json")
      .Build()
