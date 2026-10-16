using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TheaterApi.Data;

namespace TheaterApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    private readonly AppDbContext _db;

    public TestController(AppDbContext db) => _db = db;

    // GET /api/test
    [HttpGet]
    public async Task<ActionResult<string>> Get(CancellationToken ct)
    {
        // AsNoTracking: we only read, so skip EF's change tracking overhead.
        var rows = await _db.Tests
            .AsNoTracking()
            .ToListAsync();

        if (rows is null)
            return NotFound("No rows found in test.");

        return Ok(rows);
    }
}