using Dapr.Actors.Runtime;
using Microsoft.Extensions.Caching.Memory;

namespace Dapr.Decorators.InMemory
{
    public class ActorStateManager : IActorStateManager
    {
        private static readonly MemoryCache cache = new(new MemoryCacheOptions());

        public ActorStateManager(string actorId)
        {
            ActorId = actorId;
        }

        public string ActorId { get; }

        private string GetKey(string stateName)
        {
            return $"{ActorId}_{stateName}";
        }

        public Task<T> AddOrUpdateStateAsync<T>(string stateName, T addValue, Func<string, T, T> updateValueFactory, CancellationToken cancellationToken = default)
        {
            var key = GetKey(stateName);
            var item = cache.Get<T>(key);
            if (item == null)
            {
                item = updateValueFactory(key, item);
            }

            cache.Set(key, item);

            return Task.FromResult(item);

        }

        public Task AddStateAsync<T>(string stateName, T value, CancellationToken cancellationToken = default)
        {
            cache.Set(GetKey(stateName), value);

            return Task.CompletedTask;
        }

        public Task ClearCacheAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> ContainsStateAsync(string stateName, CancellationToken cancellationToken = default)
        {
            var result = cache.TryGetValue(GetKey(stateName), out var item);
            return Task.FromResult(result);
        }

        public Task<T> GetOrAddStateAsync<T>(string stateName, T value, CancellationToken cancellationToken = default)
        {
            var result = cache.GetOrCreate(GetKey(stateName), (ICacheEntry x) => value);
            return Task.FromResult(result);
        }

        public Task<T> GetStateAsync<T>(string stateName, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(cache.Get<T>(GetKey(stateName)));
        }

        public Task RemoveStateAsync(string stateName, CancellationToken cancellationToken = default)
        {
            cache.Remove(GetKey(stateName));

            return Task.CompletedTask;
        }

        public Task SaveStateAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task SetStateAsync<T>(string stateName, T value, CancellationToken cancellationToken = default)
        {
            cache.Set(GetKey(stateName), value);

            return Task.CompletedTask;
        }

        public Task<bool> TryAddStateAsync<T>(string stateName, T value, CancellationToken cancellationToken = default)
        {
            var key = GetKey(stateName);
            var item = cache.Get<T>(key);
            if (item == null)
            {
                cache.Set(key, item);
                return Task.FromResult(true);
            }
            else
            {
                return Task.FromResult(false);
            }
        }

        public Task<ConditionalValue<T>> TryGetStateAsync<T>(string stateName, CancellationToken cancellationToken = default)
        {
            if (cache.TryGetValue(GetKey(stateName), out var value))
            {
                return Task.FromResult(new ConditionalValue<T>(true, (T)value));
            }
            else
            {
                return Task.FromResult(new ConditionalValue<T>());
            }

        }

        public Task<bool> TryRemoveStateAsync(string stateName, CancellationToken cancellationToken = default)
        {
            if (cache.TryGetValue(GetKey(stateName), out var value))
            {
                cache.Remove(GetKey(stateName));
                return Task.FromResult(true);
            }
            else
            {
                return Task.FromResult(false);
            }
        }
    }
}
