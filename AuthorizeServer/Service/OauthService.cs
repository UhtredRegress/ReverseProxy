using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using AuthorizeServer.DTO;
using AuthorizeServer.Service.Interface;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;

namespace AuthorizeServer.Service;

public class OauthService : IOauthService
{
    private readonly IDatabase _redis;
    private readonly IConfiguration _configuration;

    public OauthService(IConnectionMultiplexer connectionMultiplexer, IConfiguration configuration)
    {
        _redis = connectionMultiplexer.GetDatabase();
        _configuration = configuration;
    }


    public async Task<RequestAuthDto> ValidateRequestAsync(string key, string redirectUri)
    {
        var json = await _redis.StringGetAsync(key);
        if ( json.HasValue == false)
        {
            throw new InvalidOperationException("Key is not valid");
        }
        
        var userInfo = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
        var authClaims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, userInfo["username"].ToString() ?? throw new ArgumentNullException()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            expires: DateTime.UtcNow.AddMinutes(5),
            claims: authClaims,
            signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
        );
        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
        var refreshToken = GenerateRefreshToken();
        return new RequestAuthDto {AccessToken = accessToken, RefreshToken = refreshToken};
    }

    private string GenerateRefreshToken()
    {
        var authClaims = new List<Claim>
        {
            new Claim("type", "refresh_token"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            expires: DateTime.UtcNow.AddMinutes(30),
            claims: authClaims,
            signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
        );
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}