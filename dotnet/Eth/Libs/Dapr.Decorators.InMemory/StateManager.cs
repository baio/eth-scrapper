using Dapr.Abstracts;
using Dapr.Client;
using Microsoft.Extensions.Caching.Memory;

namespace Dapr.Decorators.InMemory
{
    internal class StateEnrty<TValue> : IStateEntry<TValue>
    {
        internal StateEnrty(string key, TValue val, string etag)
        {
            Key = key;
            Value = val;
            ETag = etag;
        }

        public TValue Value { get; set; }
        public string ETag { get; }
        public string Key { get; }
    }

    public class StateManager : IStateManager
    {
        private readonly MemoryCache cache = new(new MemoryCacheOptions());

        private string GetKey(string storeName, string key) => storeName + "_" + key;

        public Task Delete(string storeName, string key, StateOptions? stateOptions = null, IReadOnlyDictionary<string, string>? metadata = null, CancellationToken cancellationToken = default)
        {
            cache.Remove(GetKey(storeName, key));

            return Task.CompletedTask;
        }

        public Task<TValue> Get<TValue>(string storeName, string key, ConsistencyMode? consistencyMode = null, IReadOnlyDictionary<string, string>? metadata = null, CancellationToken cancellationToken = default)
        {
            var val = cache.Get<IStateEntry<TValue>>(GetKey(storeName, key));

            return Task.FromResult(val.Value);
        }

        public Task<IStateEntry<TValue>> GetEntry<TValue>(string storeName, string key, ConsistencyMode? consistencyMode = null, IReadOnlyDictionary<string, string>? metadata = null, CancellationToken cancellationToken = default)
        {
            var val = cache.Get<IStateEntry<TValue>>(GetKey(storeName, key));

            return Task.FromResult(val);
        }

        public Task Save<TValue>(string storeName, string key, TValue value, StateOptions? stateOptions = null, IReadOnlyDictionary<string, string>? metadata = null, CancellationToken cancellationToken = default)
        {
            var k = GetKey(storeName, key);

            var entry = new StateEnrty<TValue>(k, value, "-1");

            cache.Set(k, entry);

            return Task.CompletedTask;
        }

        public Task<bool> TrySave<TValue>(string storeName, string key, TValue value, string etag, StateOptions? stateOptions = null, IReadOnlyDictionary<string, string>? metadata = null, CancellationToken cancellationToken = default)
        {
            var k = GetKey(storeName, key);

            var val = cache.Get<IStateEntry<TValue>>(GetKey(storeName, key));

            if (val == null || val.ETag.CompareTo(etag) == -1)
            {
                var entry = new StateEnrty<TValue>(k, value, etag);

                cache.Set(k, entry);

                return Task.FromResult(true);
            }
            else
            {
                return Task.FromResult(false);
            }

        }
    }
}
