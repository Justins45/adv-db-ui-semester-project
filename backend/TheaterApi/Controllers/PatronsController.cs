using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TheaterApi.Data;
using TheaterApi.Dtos;
using TheaterApi.Services;

namespace TheaterApi.Controllers;

[ApiController, Authorize(Roles = "PATRON,STAFF,ADMIN"), Route("api/patrons/me/preferences")]
public sealed class PatronPreferencesController(PatronService patrons) : ControllerBase
{
    /// <summary>Handles the GET request for this resource.</summary>
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct) { var id = Id(); var json = await patrons.GetPreferencesAsync(id, ct); return json is null ? NotFound() : Content(json, "application/json"); }
    /// <summary>Handles the PATCH request for this resource.</summary>
    [HttpPatch]
    public async Task<IActionResult> Replace(JsonElement request, CancellationToken ct) { try { return await patrons.SetPreferencesAsync(Id(), request.GetRawText(), ct) ? NoContent() : NotFound(); } catch (InvalidPreferencesException e) { return BadRequest(new ProblemDetails { Title = "Invalid preferences", Detail = e.Message, Status = 400 }); } }
    private int Id() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}

[ApiController, Authorize(Roles = "STAFF,ADMIN"), Route("api/admin/patrons")]
public sealed class AdminPatronsController(AppDbContext db, PatronService patrons) : ControllerBase
{
    /// <summary>Handles the GET request for this resource.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PatronSummaryDto>>> List([FromQuery] bool? wheelchair, CancellationToken ct)
    {
        var query = db.Patrons.AsNoTracking().Include(x => x.Person).AsQueryable();
        if (wheelchair == true) query = query.Where(x => EF.Functions.JsonContains(x.Preferences, "{\"accessibility\":{\"wheelchair_accessible\":true}}"));
        return Ok(await query.OrderBy(x => x.Id).Select(x => new PatronSummaryDto(x.Id, x.Person.FirstName, x.Person.LastName, x.Person.Email, x.Preferences, x.PointsBalance)).ToListAsync(ct));
    }
    /// <summary>Handles the POST request for {id:int}/points.</summary>
    [HttpPost("{id:int}/points")]
    public async Task<IActionResult> Points(int id, PointsRequest request, CancellationToken ct) => await patrons.ChangePointsAsync(id, request.Amount, ct) ? NoContent() : BadRequest(new ProblemDetails { Title = "Points change rejected", Detail = "The patron does not exist or the balance would become negative.", Status = 400 });
}
