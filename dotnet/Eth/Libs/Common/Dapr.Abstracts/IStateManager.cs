using Dapr.Client;

namespace Dapr.Abstracts
{
    public interface IStateEntry<TValue>
    {
        string Key { get; }
        TValue Value { get; set; }
        string ETag { get; }
    }

    public interface IStateManager
    {
        Task<bool> TrySave<TValue>(string storeName, string key, TValue value, string etag, StateOptions? stateOptions = null, IReadOnlyDictionary<string, string>? metadata = null, CancellationToken cancellationToken = default);
        Task Save<TValue>(string storeName, string key, TValue value, StateOptions? stateOptions = null, IReadOnlyDictionary<string, string>? metadata = null, CancellationToken cancellationToken = default);
        Task<IStateEntry<TValue>> GetEntry<TValue>(string storeName, string key, ConsistencyMode? consistencyMode = default, IReadOnlyDictionary<string, string>? metadata = default, CancellationToken cancellationToken = default);
        Task<TValue> Get<TValue>(string storeName, string key, ConsistencyMode? consistencyMode = default, IReadOnlyDictionary<string, string>? metadata = default, CancellationToken cancellationToken = default);
        Task Delete(string storeName, string key, StateOptions? stateOptions = default, IReadOnlyDictionary<string, string>? metadata = default, CancellationToken cancellationToken = default);
    }

}
