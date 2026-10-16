using Microsoft.AspNetCore.Mvc;
using TheaterApi.Data;
using TheaterApi.Models;

namespace TheaterApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LogsController(ILogStore logs) : ControllerBase
{
    [HttpPost("test")]
    public async Task<IActionResult> WriteTest()
    {
        await logs.WriteAsync(new LogEntry { Message = "Firestore connection works" });
        return Ok();
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<LogEntry>>> GetRecent() =>
        Ok(await logs.GetRecentAsync());
}