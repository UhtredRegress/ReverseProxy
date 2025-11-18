using Microsoft.AspNetCore.Mvc;

namespace AuthenticationService.Controller;

[ApiController]
[Route("/callback")]
public class CallbackController:ControllerBase
{
    private readonly ILogger<CallbackController> _logger;

    public CallbackController(ILogger<CallbackController> logger)
    {
        _logger = logger;
    }
    
    [HttpGet]
    public IActionResult ReceiveAuthCode([FromQuery]string? code, [FromQuery] string? error,  [FromQuery] string? state)
    {
        _logger.LogInformation("Received auth code successfully");

        var sessionState = HttpContext.Session.GetString("state");
        if (state != sessionState)
        {
            return BadRequest(new { Error = "Not valid function call" });
        }

        if (string.IsNullOrEmpty(code) && !string.IsNullOrEmpty(error))
        {
            return Ok(new { Error = error, State = state });
        }
        else if (!string.IsNullOrEmpty(code) && string.IsNullOrEmpty(error))
        {
            return Ok(new {Code = code, Error = error});
        }
        
        return BadRequest(new {Error = "Not valid function call" });
    }
    
}