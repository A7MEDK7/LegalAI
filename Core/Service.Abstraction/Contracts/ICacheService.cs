using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Abstraction.Contracts {
    public interface ICacheService {
        public Task<string?> GetCachedValueAsync(string key); // Get Cached Value By Key
        public Task SetCacheValueAsync(string key, object value, TimeSpan duration); // Store Value In Cache With Expiration
        Task RemoveCacheValueAsync(string key); // Remove Data With Key
    }
}
