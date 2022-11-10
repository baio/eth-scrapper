namespace ScrapperTestContext

open ScrapperModels.ScrapperStore
open ScrapperElasticStoreActor
open System.Threading.Tasks

type StoreActor(env) as this =
  let actor = ScrapperElasticStoreBaseActor(env) :> IScrapperStoreActor


  let lockt (fn: 'a -> Task<_>) (x: 'a) =
    task { return lock this (fun () -> (fn x).Result) }

  interface IScrapperStoreActor with

    member this.Store data = lockt actor.Store data
