using Microsoft.AspNetCore.Mvc;

namespace MiddleMan.Controllers;

[Route("[controller]")]
[ApiController]
public class CarsController : ControllerBase
{
    [HttpGet]
    public IActionResult GetCars()
    {
        return Ok("Cars");
    }
}