using Microsoft.AspNetCore.Mvc;

namespace App.Controllers;

[ApiController]
[Route("v1/info")]
public sealed class InfoController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok();
}
