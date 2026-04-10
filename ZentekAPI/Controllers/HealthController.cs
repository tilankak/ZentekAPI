using Microsoft.AspNetCore.Mvc;

namespace ZentekAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public IActionResult Get()
    {
        return Ok(new
        {
            Status = "OK",
            Service = "Products API",
            Timestamp = DateTime.UtcNow,
            Version = "1.0.0"
        });
    }
}
