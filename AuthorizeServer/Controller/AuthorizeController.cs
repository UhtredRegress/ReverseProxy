using AuthorizeServer.Service.Interface;
using Microsoft.AspNetCore.Mvc;


namespace AuthorizeServer.Controller;

[ApiController]
[Route("api/oauth")]
public class AuthorizeController:ControllerBase
{
    private readonly ILogger<AuthorizeController> _logger;
    private readonly IOauthService _oauthService;
    public AuthorizeController(ILogger<AuthorizeController> logger, IOauthService oauthService)
    {
        _logger = logger;
        _oauthService = oauthService;
    }

    [HttpGet]
    public async Task<IActionResult> RequestOauth([FromQuery]string key, [FromQuery] string redirect_uri)
    {
        _logger.LogInformation("Receive request to get auth token");
        try
        {
            var jwt = await _oauthService.ValidateRequestAsync(key, redirect_uri);
            Response.Cookies.Append("access_token", jwt.AccessToken, new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddMinutes(5)
            });
            Response.Cookies.Append("refresh_token", jwt.RefreshToken, new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddMinutes(30)  
            });
            return Redirect(redirect_uri);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}