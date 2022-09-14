namespace Infra.AzureTable

[<AutoOpen>]
module TableClient =
    open Azure.Data.Tables

    let azureTableClient (connectionString: string, tableName: string) =
        lazy (TableClient(connectionString, tableName))
