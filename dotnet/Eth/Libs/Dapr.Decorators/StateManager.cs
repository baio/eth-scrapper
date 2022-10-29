using Dapr.Abstracts;
using Dapr.Client;

namespace Dapr.Decorators
{

    public class StateEntry<T> : IStateEntry<T>
    {
        private readonly Dapr.StateEntry<T> stateEntry;

        public StateEntry(Dapr.StateEntry<T> stateEntry)
        {
            this.stateEntry = stateEntry;
        }

        public string Key => stateEntry.Key;

        public T Value { get => stateEntry.Value; set => stateEntry.Value = value; }

        public string ETag => stateEntry.ETag;

    }

    public class StateManager : IStateManager
    {
        public StateManager(DaprClient daprClient)
        {
            DaprClient = daprClient;
        }

        public DaprClient DaprClient { get; }

        public Task DeleteStateAsync(string storeName, string key, StateOptions? stateOptions = null, IReadOnlyDictionary<string, string>? metadata = null, CancellationToken cancellationToken = default)
        {
            return DaprClient.DeleteStateAsync(storeName, key, stateOptions, metadata, cancellationToken);
        }

        public Task<TValue> Get<TValue>(string storeName, string key, ConsistencyMode? consistencyMode = null, IReadOnlyDictionary<string, string>? metadata = null, CancellationToken cancellationToken = default)
        {
            return DaprClient.GetStateAsync<TValue>(storeName, key, consistencyMode, metadata, cancellationToken);
        }

        public async Task<IStateEntry<TValue>> GetEntry<TValue>(string storeName, string key, ConsistencyMode? consistencyMode = null, IReadOnlyDictionary<string, string>? metadata = null, CancellationToken cancellationToken = default)
        {
            var result = await DaprClient.GetStateEntryAsync<TValue>(storeName, key, consistencyMode, metadata, cancellationToken);

            return new StateEntry<TValue>(result);
        }

        public Task Save<TValue>(string storeName, string key, TValue value, StateOptions? stateOptions = null, IReadOnlyDictionary<string, string>? metadata = null, CancellationToken cancellationToken = default)
        {
            return DaprClient.SaveStateAsync<TValue>(storeName, key, value, stateOptions, metadata, cancellationToken);
        }

        public Task<bool> TrySave<TValue>(string storeName, string key, TValue value, string etag, StateOptions? stateOptions = null, IReadOnlyDictionary<string, string>? metadata = null, CancellationToken cancellationToken = default)
        {
            return DaprClient.TrySaveStateAsync<TValue>(storeName, key, value, etag, stateOptions, metadata, cancellationToken);
        }
    }
}
