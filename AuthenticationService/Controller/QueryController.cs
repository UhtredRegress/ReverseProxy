using AuthenticationService.Service.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthenticationService.Controller;

[Authorize]
[ApiController]
[Route("api/query")]
public class QueryController : ControllerBase
{
    private readonly ILogger<QueryController> _logger;
    private readonly IQueryService _queryService;
    
    public QueryController(ILogger<QueryController> logger, IQueryService queryService)
    {
        _logger = logger;
        _queryService = queryService;
    }

    [HttpGet]
    public async Task<IActionResult> CheckPaid(string scope, string guid)
    {
        _logger.LogInformation("Receive request to check paid {scope} - {guid}", scope, guid);
        try
        {
            var result = await _queryService.CheckPaid(scope, guid);
            return Ok(new { data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking paid {scope}", scope);
            return BadRequest(new { error = ex.Message });
        }
    } 
}