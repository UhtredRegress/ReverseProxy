using System.Net.Http.Headers;
using System.Security.Cryptography;
using AuthenticationService.Controller;
using AuthenticationService.Models;
using AuthenticationService.Service.Interface;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace AuthenticationService.Service;

public class RequestService : IRequestService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly HttpClient _httpClient;
    private readonly IRedisCacheService _redisCacheService;

    public RequestService(ApplicationDbContext dbContext, HttpClient httpClient, IRedisCacheService redisCacheService)
    {
        _dbContext = dbContext;
        _httpClient = httpClient;
        _redisCacheService = redisCacheService;
    }

    public async Task<string> RequestRedirect(string guid, string accessToken, string scope)
    {
        ArgumentException.ThrowIfNullOrEmpty(accessToken, "accessToken");
        ArgumentException.ThrowIfNullOrEmpty(scope, "scope");
        ArgumentException.ThrowIfNullOrEmpty(guid, "Uid");
        
        var result = await _dbContext.ClientAppDetails
            .Where(c => c.Type.Type == scope  && c.User.Id == guid)
            .Include(c => c.Type)
            .AsNoTracking()
            .FirstOrDefaultAsync();
        


        if (result == null || result.Purchased == false)
        {
            throw new UnauthorizedAccessException("The user is not authorized to redirect to this resource");
        }

        string baseUrl = "http://localhost:5046/api/oauth";
        string key = Guid.NewGuid().ToString();
        
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == guid);

        var info = new Dictionary<string, string>
        {
            { "username", user.Email }
        };
        
        await _redisCacheService.SetAsync(key, info, TimeSpan.FromMinutes(5));
        
        var queryParams = new Dictionary<string, string>
        {
            ["key"] = key,
            ["redirect_uri"] = result.Type.RedirectUri
        };
        
        
        return QueryHelpers.AddQueryString(baseUrl, queryParams);
       
    }
    
}