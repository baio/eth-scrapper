namespace ScrapperTestContext

open ScrapperModels.ScrapperStore
open ScrapperElasticStoreActor
open System.Threading.Tasks

type StoreActor(env) =
  let actor = ScrapperElasticStoreBaseActor(env) :> IScrapperStoreActor

  let mailbox = createMailbox ()

  interface IScrapperStoreActor with

    member this.Store data = sync mailbox <@ actor.Store @> data
