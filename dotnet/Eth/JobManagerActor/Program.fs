
#nowarn "20"

module Program =
  open Common.DaprActor
  open ScrapperModels.ScrapperDispatcherActor

  [<EntryPoint>]
  let main args =
    createDaprActor (fun opts -> opts.Actors.RegisterActor<ScrapperDispatcherActor>()) args
