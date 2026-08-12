using Domain.Contract;
using Microsoft.EntityFrameworkCore.Storage;
using StackExchange.Redis;
using System.Text.Json;
using IDatabase = StackExchange.Redis.IDatabase;

namespace Persistence.Repositories {
    public class CacheRepository(IConnectionMultiplexer _connectionMultiplexer) : ICacheRepository {

        // Lazy property: get database only if Redis is connected
        private IDatabase? Database => _connectionMultiplexer is { IsConnected: true } ? _connectionMultiplexer.GetDatabase() : null;
        public async Task<string?> GetAsync(string key) {
            try {
                // Try to get Redis DB instance
                var _database = Database;
                // If Redis is down → skip caching completely
                if (_database is null) return null;
                // Get value from Redis
                var value = await _database.StringGetAsync(key);
                // Return null if not found or empty
                return value.IsNullOrEmpty ? default : value;
            } catch {
                // If Redis throws any exception → ignore and fallback to DB
                return null;
            }
        }
        public async Task SetAsync(string key, object value, TimeSpan duration) {
            try {
                // Get Redis database instance
                var database = Database;

                // If Redis is unavailable, skip caching silently
                if (database is null)
                    return;

                string serializedObject;

                // ------------------------------------------------------------
                // Check if value is already a JSON string
                // ------------------------------------------------------------
                if (value is string str) {
                    serializedObject = str;
                }
                else {
                    // Serialize object only once before storing in Redis
                    serializedObject = JsonSerializer.Serialize(value);
                }

                // Store serialized value in Redis with expiration time
                await database.StringSetAsync(key, serializedObject, duration);
            } catch {
                // Fail silently if Redis is down or any error occurs
                // (Caching should never break the main application flow)
            }
        }
        public async Task RemoveAsync(string key) {
            try {
                // Try to get Redis DB instance
                var _database = Database;
                // If Redis is down -> skip caching completely
                if (_database is null) return;
                // Get value from Redis
                await _database.KeyDeleteAsync(key);
            } catch {
                // If Redis throws any exception → ignore and fallback to DB
                return;
            }
        }
    }
}
