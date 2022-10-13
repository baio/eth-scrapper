
#nowarn "20"

module Program =
  open Common.DaprActor
  open JobManager

  [<EntryPoint>]
  let main args =
    createDaprActor (fun opts -> opts.Actors.RegisterActor<JobManagerActor>()) args
