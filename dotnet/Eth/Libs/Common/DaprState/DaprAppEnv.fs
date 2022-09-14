namespace Common.DaprState

open Dapr.Client
open Microsoft.Extensions.Logging

type DaprAppEnv = { Logger: ILogger; Dapr: DaprClient }

type DaprStoreEnv = { App: DaprAppEnv; StoreName: string }
