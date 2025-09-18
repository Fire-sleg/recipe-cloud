
using Microsoft.Extensions.Caching.StackExchangeRedis;
using RecipeService.Controllers;
using RecipeService.Models.Categories.DTOs;
using StackExchange.Redis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RecipeService.Services
{
    public class RedisCache : IRedisCache
    {
        private readonly IDatabase _redis;
        private readonly JsonSerializerOptions _jsonOptions;
        public RedisCache(IConnectionMultiplexer redis) 
        { 
            _redis = redis.GetDatabase();

            _jsonOptions = new JsonSerializerOptions
            {
                ReferenceHandler = ReferenceHandler.IgnoreCycles,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }
        public async Task<T?> GetAsync<T>(string key)
        {
            var cached = await _redis.StringGetAsync(key);

            if (!cached.HasValue) return default;

            try
            {
                var value = JsonSerializer.Deserialize<T>(cached, _jsonOptions);
                return value;
            }
            catch (Exception ex)
            {
                await _redis.KeyDeleteAsync(key);
                return default;
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
        {
            var serialized = JsonSerializer.Serialize(value, _jsonOptions);
            await _redis.StringSetAsync(key, serialized, expiry);
        }

        public async Task RemoveAsync(string key)
        {
            await _redis.KeyDeleteAsync(key);
        }

        public async Task<bool> ExistsAsync(string key)
        {
            return await _redis.KeyExistsAsync(key);
        }
    }
}
