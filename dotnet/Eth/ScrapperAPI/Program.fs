namespace ScrapperAPI

module Program =
  [<EntryPoint>]
  let main args =
    Common.DaprAPI.DaprAPI.createDaprAPI args
