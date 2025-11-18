namespace AuthenticationService.Service.Interface;

public interface IRedisCacheService
{
    Task SetAsync(string key, Dictionary<string, string> value, TimeSpan ttl);
}