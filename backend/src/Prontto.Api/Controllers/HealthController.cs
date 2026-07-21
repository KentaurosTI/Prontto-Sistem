using Microsoft.AspNetCore.Mvc;

namespace Prontto.Api.Controllers;

[ApiController]
public class HealthController : ControllerBase
{
    [HttpGet("/healthz")]
    public IActionResult Health() => Ok(new { status = "ok" });
}
