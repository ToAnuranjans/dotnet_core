using Microsoft.AspNetCore.Mvc;

namespace MiddleMan.Controllers;

[Route("[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult GetHealth()
    {
        return Ok("Healthy");
    }
}
