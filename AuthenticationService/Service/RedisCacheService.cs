using System.Text.Json;
using AuthenticationService.Service.Interface;
using StackExchange.Redis;

namespace AuthenticationService.Service;

public class RedisCacheService : IRedisCacheService
{
    private readonly IDatabase _db;
    public RedisCacheService(IConnectionMultiplexer redis, IConfiguration configuration)
    {
        _db = redis.GetDatabase();
    }
    
    public async Task SetAsync(string key, Dictionary<string, string> value, TimeSpan ttl)
    {
        var json = JsonSerializer.Serialize(value);
        await _db.StringSetAsync(key, json, ttl);
    }
}