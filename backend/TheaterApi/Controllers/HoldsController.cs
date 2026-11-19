using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TheaterApi.Dtos;
using TheaterApi.Services;

namespace TheaterApi.Controllers;
[ApiController, Authorize(Roles = "PATRON,STAFF,ADMIN"), Route("")]
public sealed class HoldsController(HoldService holds) : ControllerBase
{
    [HttpPost("performances/{performanceId:int}/holds")]
    public async Task<ActionResult<HoldDto>> Create(int performanceId, CreateHoldRequest request, CancellationToken ct) { try { return Ok(await holds.CreateAsync(PersonId(), performanceId, request.SeatIds, ct)); } catch (KeyNotFoundException e) { return NotFound(new ProblemDetails { Title = "Performance not found", Detail = e.Message, Status = 404 }); } catch (HoldConflictException e) { return Conflict(new ProblemDetails { Title = "Seat hold conflict", Detail = e.Message, Status = 409 }); } }
    [HttpGet("holds/current")] public async Task<ActionResult<HoldDto>> Current(CancellationToken ct) => (await holds.CurrentAsync(PersonId(), ct)) is { } hold ? Ok(hold) : NoContent();
    [HttpDelete("holds/{id:int}")] public async Task<IActionResult> Delete(int id, CancellationToken ct) => await holds.DeleteAsync(PersonId(), id, ct) ? NoContent() : NotFound();
    private int PersonId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
