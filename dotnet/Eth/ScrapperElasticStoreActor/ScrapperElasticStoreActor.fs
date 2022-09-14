namespace ScrapperElasticStoreActor

[<AutoOpen>]
module ScrapperStoreActor =

  open Dapr.Actors
  open Dapr.Actors.Runtime
  open ScrapperModels
  open Microsoft.Extensions.Configuration
  open Elasticsearch.Net
  open Microsoft.Extensions.Logging
  open Common.DaprActor

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

  let runScrapperDispatcherContinue (proxyFactory: Client.IActorProxyFactory) id (data: ContinueSuccessData) =
    let actor =
      proxyFactory.CreateActorProxy<IScrapperDispatcherActor>(id, "scrapper-dispatcher")

    let success = data.Result

    let continueData: ContinueData =
      { EthProviderUrl = data.EthProviderUrl
        ContractAddress = data.ContractAddress
        Abi = data.Abi
        Result =
          (Ok
            { BlockRange = success.BlockRange
              RequestBlockRange = success.RequestBlockRange }) }

    actor.Continue continueData |> ignore

  let runScrapperDispatcherFailure (proxyFactory: Client.IActorProxyFactory) id =
    let actor =
      proxyFactory.CreateActorProxy<IScrapperDispatcherActor>(id, "scrapper-dispatcher")


    let failureData: FailureData =
      { AppId = AppId.ElasticStore
        Status = StoreFailure "Failed to store in elasticsearch" }

    actor.Failure failureData |> ignore


  [<Actor(TypeName = "scrapper-elastic-store")>]
  type ScrapperElasticStoreActor(host: ActorHost, config: IConfiguration) =
    inherit Actor(host)
    let logger = ActorLogging.create host

    interface IScrapperStoreActor with
      member this.Store data =
        task {
          let config = elasticConfig config
          let! result = store logger config data.Result.IndexPayload

          match result with
          | true -> runScrapperDispatcherContinue this.ProxyFactory this.Id data
          | false -> runScrapperDispatcherFailure this.ProxyFactory this.Id

          return result
        }
