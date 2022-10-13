namespace ScrapperElasticStoreActor

[<AutoOpen>]
module ScrapperStoreActor =

  open Dapr.Actors
  open Dapr.Actors.Runtime
  open ScrapperModels.ScrapperDispatcher
  open ScrapperModels.ScrapperStore
  open Microsoft.Extensions.Configuration
  open Elasticsearch.Net
  open Microsoft.Extensions.Logging
  open Common.DaprActor
  open ScrapperModels

  let elasticConfig (config: IConfiguration) =
    let connectionString = config.GetConnectionString("ElasticSearch")
    connectionString

  let store (logger: ILogger) (elasticConfig: string) (indexPayload: string) =

    logger.LogDebug("Store scrapper payload to elasticsearch {config}", elasticConfig)


    task {
      try

        let elasticConfig = new ConnectionConfiguration(System.Uri(elasticConfig))
        let client = ElasticLowLevelClient(elasticConfig)

        let data = PostData.String(indexPayload)

        let! response = client.BulkAsync<VoidResponse> data

        logger.LogInformation(
          "Store scrapper payload to elasticsearch success {success} {@error}",
          response.Success,
          response.OriginalException
        )

        if not response.Success then
          raise response.OriginalException

        return true
      with
      | _ as err ->
        logger.LogError("Store scrapper payload to elasticsearch error {@error}", err)
        return false

    }

  [<Actor(TypeName = "scrapper-elastic-store")>]
  type ScrapperElasticStoreActor(host: ActorHost, config: IConfiguration) =
    inherit Actor(host)
    let logger = ActorLogging.create host

    let createScrapperDispatcherActor (JobId id) =
      host.ProxyFactory.CreateActorProxy<IScrapperDispatcherActor>((ActorId id), "scrapper-dispatcher")

    let elasticConfig = elasticConfig config

    let env: Env =
      { Logger = logger
        Store = store logger elasticConfig
        ActorId = JobId(host.Id.ToString()) 
        CreateScrapperDispatcherActor = createScrapperDispatcherActor }

    let actor = ScrapperElasticStoreBaseActor env :> IScrapperStoreActor

    interface IScrapperStoreActor with
      member this.Store data = actor.Store data
