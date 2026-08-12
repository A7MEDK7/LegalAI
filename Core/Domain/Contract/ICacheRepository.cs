using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Contract {
    public interface ICacheRepository {
        public Task<string?> GetAsync(string key); // Get Cached Value By Key (Returns Serialized String)
        public Task SetAsync(string key, object value, TimeSpan duration); // Set Value In Cache With Expiration Duration
        public Task RemoveAsync(string key); // Remove Cached Value By Key
    }
}
