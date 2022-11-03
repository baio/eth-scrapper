namespace Scrapper.Repo

open Common.DaprState


type RepoEnv =
  { StateEnv: StateEnv
    Now: unit -> System.DateTime }
