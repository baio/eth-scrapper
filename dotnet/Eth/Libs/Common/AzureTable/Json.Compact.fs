module Json.Compact

open Microsoft.FSharpLu.Json
open Newtonsoft.Json
open Newtonsoft.Json.Linq
open Newtonsoft.Json.Serialization

let private compactJsonSerializerSettings =
    let settings = Compact.TupleAsArraySettings.settings
    settings.ContractResolver <- CamelCasePropertyNamesContractResolver()
    settings.MissingMemberHandling <- MissingMemberHandling.Ignore
    settings.Formatting <- Formatting.None
    settings

let private compactJsonSerializer =
    JsonSerializer.Create compactJsonSerializerSettings

let toJToken o =
    JToken.FromObject(o, compactJsonSerializer)

let fromJToken<'T> (token: JToken) =
    token.ToObject<'T> compactJsonSerializer

let serialize o =
    JsonConvert.SerializeObject(o, compactJsonSerializerSettings)

let deserialize<'T> json =
    JsonConvert.DeserializeObject<'T>(json, compactJsonSerializerSettings)
