namespace ScrapperElasticStoreActor

#nowarn "20"

open Common.DaprActor

module Program =


  [<EntryPoint>]
  let main args =
    createDaprActor (fun opts -> opts.Actors.RegisterActor<ScrapperElasticStoreActor>()) args
