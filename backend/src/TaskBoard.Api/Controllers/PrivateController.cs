using Microsoft.AspNetCore.Mvc;

namespace TaskBoard.Api.Controllers;

[ApiController]
[Route("api/private")]
public sealed class PrivateController : ControllerBase
{
    [HttpGet("ping")]
    public IActionResult Ping()
    {
        return Ok(new { message = "pong" });
    }
}
