using Domain.Contract;
using Service.Abstraction.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Service.Implementations {
    public class CacheService(ICacheRepository cacheRepository) : ICacheService {

        private static readonly JsonSerializerOptions _options = new() {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DictionaryKeyPolicy = JsonNamingPolicy.CamelCase
        };

        public async Task<string?> GetCachedValueAsync(string key)
            => await cacheRepository.GetAsync(key);

        public async Task SetCacheValueAsync(string key, object value, TimeSpan duration) {
            var json = JsonSerializer.Serialize(value, _options);
            await cacheRepository.SetAsync(key, json, duration);
        }

        public async Task RemoveCacheValueAsync(string key)
            => await cacheRepository.RemoveAsync(key);
    }
}
