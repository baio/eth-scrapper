namespace Common.DaprState

open Dapr.Client

type DaprStateManager(dapr: DaprClient) =

  let wrapNullable x = x |> Option.defaultValue null

  let wrapNullable' (x: System.Nullable<_> option) =
    match x with
    | Some x -> x
    | None -> System.Nullable()

  let wrapCancelationToken =
    function
    | None -> System.Threading.CancellationToken.None
    | Some x -> x

  interface IStateManager with
    member this.delete
      (
        storeName: string,
        key: string,
        ?stateOptions: StateOptions,
        ?metadata: System.Collections.Generic.IReadOnlyDictionary<string, string>,
        ?cancellationToken: System.Threading.CancellationToken
      ) : System.Threading.Tasks.Task =
      dapr.DeleteStateAsync(
        storeName,
        key,
        stateOptions = wrapNullable stateOptions,
        metadata = wrapNullable metadata,
        cancellationToken = wrapCancelationToken cancellationToken
      )

    member this.get
      (
        storeName: string,
        key: string,
        consistencyMode: System.Nullable<ConsistencyMode> option,
        metadata: System.Collections.Generic.IReadOnlyDictionary<string, string> option,
        cancellationToken: System.Threading.CancellationToken option
      ) : System.Threading.Tasks.Task<'TValue> =
      dapr.GetStateAsync(
        storeName,
        key,
        consistencyMode = wrapNullable' consistencyMode,
        metadata = wrapNullable metadata,
        cancellationToken = wrapCancelationToken cancellationToken
      )


    member this.getEntry
      (
        storeName: string,
        key: string,
        consistencyMode: System.Nullable<ConsistencyMode> option,
        metadata: System.Collections.Generic.IReadOnlyDictionary<string, string> option,
        cancellationToken: System.Threading.CancellationToken option
      ) : System.Threading.Tasks.Task<IValue<'TValue>> =
      task {
        let! result =
          dapr.GetStateEntryAsync<'TValue>(
            storeName,
            key,
            consistencyMode = wrapNullable' consistencyMode,
            metadata = wrapNullable metadata,
            cancellationToken = wrapCancelationToken cancellationToken
          )

        return
          { new IValue<'TValue> with
              member this.Value = result.Value
              member this.ETag = result.ETag }
      }


    member this.save
      (
        storeName: string,
        key: string,
        value: 'TValue,
        stateOptions: StateOptions option,
        metadata: System.Collections.Generic.IReadOnlyDictionary<string, string> option,
        cancellationToken: System.Threading.CancellationToken option
      ) : System.Threading.Tasks.Task =
      dapr.SaveStateAsync(
        storeName,
        key,
        value,
        stateOptions = wrapNullable stateOptions,
        metadata = wrapNullable metadata,
        cancellationToken = wrapCancelationToken cancellationToken
      )


    member this.trySave
      (
        storeName: string,
        key: string,
        value: 'TValue,
        etag: string,
        stateOptions: StateOptions option,
        metadata: System.Collections.Generic.IReadOnlyDictionary<string, string> option,
        cancellationToken: System.Threading.CancellationToken option
      ) : System.Threading.Tasks.Task<bool> =
      dapr.TrySaveStateAsync(
        storeName,
        key,
        value,
        etag,
        stateOptions = wrapNullable stateOptions,
        metadata = wrapNullable metadata,
        cancellationToken = wrapCancelationToken cancellationToken
      )
