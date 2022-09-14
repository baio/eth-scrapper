namespace Infra.AzureBlob

open FSharp.Control

[<RequireQualifiedAccess>]
module BlobOperators =
  open Azure.Storage.Blobs
  open System.IO
  open System.Text

  let readAsStream (blobClient: BlobClient) = blobClient.OpenReadAsync()

  let readAsString (blobClient: BlobClient) =
    task {
      let! stream = readAsStream blobClient
      let reader = new StreamReader(stream)
      let! result = reader.ReadToEndAsync()
      return result
    }

  let writeStream (blobContainerClient: BlobContainerClient) (blobName: string) (stream: Stream) =
    blobContainerClient.UploadBlobAsync(blobName, stream)

  let writeString (blobContainerClient: BlobContainerClient) (blobName: string) (data: string) =
    use stream = new System.IO.MemoryStream(Encoding.UTF8.GetBytes(data))
    writeStream blobContainerClient blobName stream

  let readNextBlobName (blobContainerClient: BlobContainerClient) (marker: string option) =
    task {
      let result = blobContainerClient.GetBlobsAsync()

      let marker =
        match marker with
        | Some marker -> marker
        | None -> null

      let pages = result.AsPages(marker, 1)

      let! result =
        pages
        |> AsyncSeq.ofAsyncEnum
        |> AsyncSeq.firstOrDefault null

      return
        match result with
        | null -> None
        | _ as result ->
          let value = result.Values |> Seq.head
          (value.Name, result.ContinuationToken) |> Some
    }

  let readNextBlobClient (blobContainerClient: BlobContainerClient) marker =
    task {
      let! result = readNextBlobName blobContainerClient marker

      match result with
      | Some (name, marker) ->
        let blobClient = blobContainerClient.GetBlobClient name
        return (blobClient, marker) |> Some
      | None -> return None
    }

  let readNextBlob (blobContainerClient: BlobContainerClient) marker =
    task {
      let! result = readNextBlobClient blobContainerClient marker

      match result with
      | Some (blobClient, marker) ->
        let! result = readAsString blobClient
        return (result, marker) |> Some
      | None -> return None
    }
