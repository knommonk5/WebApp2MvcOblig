using Microsoft.AspNetCore.Mvc;

namespace PersonalAgent.Api.Controllers;

[ApiController]
[Route("api/status")]
public class StatusController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            application = "Personal Agent Core API",
            status = "Running"
        });
    }
}