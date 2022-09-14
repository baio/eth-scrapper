namespace StoreReaderActor

open System.Text
open System

[<AutoOpen>]
module StoreReaderActor =

  open Dapr.Actors
  open Dapr.Actors.Runtime
  open System.Threading.Tasks
  open ScrapperModels
  open Infra.AzureBlob
  open Microsoft.Extensions.Configuration
  open Elasticsearch.Net

  let getAzureBlobConfig (config: IConfiguration) =
    let connectionString = config.GetConnectionString("AzureBlob")

    connectionString

  let bulkInsert (index: string) (data: string) =
    //let uri = Uri("http://localhost:9200")

    //use settings =
    //  (new ConnectionConfiguration(uri))
    //    .CertificateFingerprint("4b17b197a9cc25b6dab263252138a0ccf8f28f981cb76885b14873aaae09feaf")

    let client = ElasticLowLevelClient() // settings

    let str =
      sprintf "{ \"index\" : { \"_index\" : \"%s\" } }\n{ \"value3\": \"value3\"}\n" index

    printfn "111 %s" str
    let data = PostData.String(str)

    task {
      try
        let! response = client.BulkAsync<StringResponse> data
        printfn "success !!! %b %O" response.Success response.OriginalException
        return ()
      with
      | _ as err ->
        printfn "error %O %O !!!!" err err.InnerException
        return raise err

    }

  let readNext azureTableConfig (id: string) (marker: string option) =

    task {
      try

        let blobContainerClient = (blobClientOpen (azureTableConfig, id)).Value

        let! result = BlobOperators.readNextBlob blobContainerClient marker

        printfn "Read next success"

        return result
      with
      | _ as err ->
        printfn "Read next error %O" err
        return raise err
    }

  //let runScrapperDispatcher (proxyFactory: Client.IActorProxyFactory) id (data: ContinueSuccessData) =
  //  //let actor =
  //  //  proxyFactory.CreateActorProxy<IScrapperDispatcherActor>(id, "scrapper-dispatcher")

  //  //let success = data.Result

  //  //let continueData: ContinueData =
  //  //  { ContractAddress = data.ContractAddress
  //  //    Abi = data.Abi
  //  //    Result = (Ok success) }

  //  //actor.Continue continueData |> ignore
  //  ()


  [<Actor(TypeName = "store-reader")>]
  type StoreReaderActor(host: ActorHost, config: IConfiguration) =
    inherit Actor(host)

    interface IStoreReaderActor with
      member this.Read data =
        task {
          let id = this.Id.ToString()
          // let azureTableConfig = getAzureBlobConfig config
          do! bulkInsert id ""
          // do! readNext azureTableConfig id data
          // runScrapperDispatcher this.ProxyFactory this.Id data
          return true
        }
