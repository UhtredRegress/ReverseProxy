using System.Security.Claims;
using AuthenticationService.Service.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthenticationService.Controller;

[Authorize]
[ApiController]
[Route("api/request")]
public class RequestController : ControllerBase
{
    private readonly ILogger<RequestController> _logger;
    private readonly IRequestService _requestService;

    public RequestController(ILogger<RequestController> logger, IRequestService requestService)
    {
        _logger = logger;
        _requestService = requestService;
    }

    [HttpGet("redirect")]
    public async Task<IActionResult> RequestRedirection(string scope)
    {
        _logger.LogInformation("Receive request redirection to scope {scope}", scope);
        var guid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        try
        {
            var uri = await _requestService.RequestRedirect(guid, scope);
            return Redirect(uri);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { status = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { status = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { status = ex.Message });
        }   
    }
}