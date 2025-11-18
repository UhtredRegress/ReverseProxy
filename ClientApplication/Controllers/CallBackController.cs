using Microsoft.AspNetCore.Mvc;

namespace ClientApplication.Controllers;

[ApiController]
[Route("api")]
public class CallBackController : ControllerBase
{
    public CallBackController()
    {
    }

    [HttpGet]
    public IActionResult CheckWorking()
    {
        return Ok("This is working.");
    }
}