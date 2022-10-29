namespace Common.DaprState

open Dapr.Client

  type IValue<'TValue> =
    abstract ETag: string
    abstract Value: 'TValue

  type IStateManager =
    abstract trySave:
      storeName: string *
      key: string *
      value: 'TValue *
      etag: string *
      ?stateOptions: StateOptions *
      ?metadata: System.Collections.Generic.IReadOnlyDictionary<string, string> *
      ?cancellationToken: System.Threading.CancellationToken ->
        System.Threading.Tasks.Task<bool>

    abstract save:
      storeName: string *
      key: string *
      value: 'TValue *
      ?stateOptions: StateOptions *
      ?metadata: System.Collections.Generic.IReadOnlyDictionary<string, string> *
      ?cancellationToken: System.Threading.CancellationToken ->
        System.Threading.Tasks.Task

    abstract getEntry:
      storeName: string *
      key: string *
      ?consistencyMode: System.Nullable<ConsistencyMode> *
      ?metadata: System.Collections.Generic.IReadOnlyDictionary<string, string> *
      ?cancellationToken: System.Threading.CancellationToken ->
        System.Threading.Tasks.Task<IValue<'TValue>>

    abstract get<'TValue> :
      storeName: string *
      key: string *
      ?consistencyMode: System.Nullable<ConsistencyMode> *
      ?metadata: System.Collections.Generic.IReadOnlyDictionary<string, string> *
      ?cancellationToken: System.Threading.CancellationToken ->
        System.Threading.Tasks.Task<'TValue>

    abstract delete:
      storeName: string *
      key: string *
      ?stateOptions: StateOptions *
      ?metadata: System.Collections.Generic.IReadOnlyDictionary<string, string> *
      ?cancellationToken: System.Threading.CancellationToken ->
        System.Threading.Tasks.Task
