namespace Infra.AzureBlob

[<AutoOpen>]
module BlobClient =
  open Azure.Storage.Blobs

  let blobClientOpen (connectionString: string, containerName: string) =
    lazy
      (let blobServiceClient = BlobServiceClient(connectionString)
       let containerClient = blobServiceClient.GetBlobContainerClient(containerName)
       containerClient)

  let blobClientOpenCreateIfNotExist (connectionString: string, containerName: string) =
    let client =
      blobClientOpen(
        connectionString,
        containerName
      )
        .Value

    task {
      let! _ = client.CreateIfNotExistsAsync()
      return client
    }


  let blobOpen (connectionString: string, containerName: string, blobName: string) =
    lazy
      (let client =
        blobClientOpen(
          connectionString,
          containerName
        )
          .Value

       client.GetBlobClient(blobName))
