namespace ScrapperDispatcherActor

#nowarn "20"

module Program =
  open Common.DaprActor

  [<EntryPoint>]
  let main args =
    createDaprActor (fun opts -> opts.Actors.RegisterActor<ScrapperDispatcherActor>()) args
