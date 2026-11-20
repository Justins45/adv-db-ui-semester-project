using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TheaterApi.Data;
using TheaterApi.Dtos;
using TheaterApi.Services;

namespace TheaterApi.Controllers;

[ApiController, Route("api")]
public sealed class CatalogueController(AppDbContext db, IPriceResolutionService prices, TimeProvider clock) : ControllerBase
{
    /// <summary>Handles the GET request for events.</summary>
    [HttpGet("events")]
    public async Task<ActionResult<IReadOnlyList<EventListDto>>> Events([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null, [FromQuery] int? genreId = null, CancellationToken ct = default)
    {
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 100);
        var q = db.Events.AsNoTracking().Include(e => e.Genres).Where(e => e.Status == "ACTIVE");
        if (!string.IsNullOrWhiteSpace(search)) q = q.Where(e => EF.Functions.ILike(e.Title, $"%{search.Trim()}%"));
        if (genreId.HasValue) q = q.Where(e => e.Genres.Any(g => g.Id == genreId));
        var result = await q.OrderBy(e => e.Id).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(e => new EventListDto(e.Id, e.Title, e.Description, e.Status, e.LiveDate, e.Genres.OrderBy(g => g.Name).Select(g => g.Name).ToList())).ToListAsync(ct);
        return Ok(result);
    }

    /// <summary>Handles the GET request for events/{id:int}.</summary>
    [HttpGet("events/{id:int}")]
    public async Task<ActionResult<EventDetailDto>> Event(int id, CancellationToken ct)
    {
        var e = await db.Events.AsNoTracking().Include(x => x.Genres).Include(x => x.Performances).SingleOrDefaultAsync(x => x.Id == id && x.Status == "ACTIVE", ct);
        return e is null ? NotFound() : Ok(new EventDetailDto(e.Id, e.Title, e.Description, e.Status, e.LiveDate, e.Genres.OrderBy(g => g.Name).Select(g => g.Name).ToList(), e.Performances.OrderBy(p => p.DateTime).Select(p => new PerformanceSummaryDto(p.Id, p.DateTime, p.Status, p.StageId, p.Filled)).ToList()));
    }

    /// <summary>Handles the GET request for events/{id:int}/image.</summary>
    [HttpGet("events/{id:int}/image")]
    public async Task<IActionResult> EventImage(int id, CancellationToken ct) { var image = await db.Events.Where(e => e.Id == id && e.Status == "ACTIVE").Select(e => e.Image).SingleOrDefaultAsync(ct); return image is null ? NotFound() : File(image, "application/octet-stream"); }

    /// <summary>Handles the GET request for genres.</summary>
    [HttpGet("genres")]
    public async Task<ActionResult<IReadOnlyList<GenreDto>>> Genres(CancellationToken ct) => Ok(await db.Genres.AsNoTracking().Where(g => g.Active).OrderBy(g => g.Name).Select(g => new GenreDto(g.Id, g.Name, g.Active)).ToListAsync(ct));

    /// <summary>Handles the GET request for performances/{id:int}.</summary>
    [HttpGet("performances/{id:int}")]
    public async Task<ActionResult<PerformanceDetailDto>> Performance(int id, CancellationToken ct) { var p = await db.Performances.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct); return p is null ? NotFound() : Ok(new PerformanceDetailDto(p.Id, p.EventId, p.StageId, p.DateTime, p.Status, p.Filled)); }

    /// <summary>Handles the GET request for performances/{id:int}/seats.</summary>
    [HttpGet("performances/{id:int}/seats")]
    public async Task<ActionResult<IReadOnlyList<PerformanceSeatDto>>> Seats(int id, CancellationToken ct)
    {
        var seats = await db.PerformanceSeats.AsNoTracking().Include(s => s.Seat).ThenInclude(s => s.Section).Include(s => s.Performance).Where(s => s.PerformanceId == id).ToListAsync(ct);
        if (seats.Count == 0 && !await db.Performances.AnyAsync(p => p.Id == id, ct)) return NotFound();
        var tiers = await db.PriceTiers.AsNoTracking().Where(t => !t.PerformanceId.HasValue || t.PerformanceId == id).ToListAsync(ct);
        var now = clock.GetUtcNow();
        return Ok(seats.OrderBy(s => s.Seat.SectionId).ThenBy(s => s.Seat.Row).ThenBy(s => s.Seat.SeatNumber).Select(s =>
        {
            var status = SeatAvailabilityStatus.GetDisplayStatus(s.Status, s.HeldUntil, now);
            return new PerformanceSeatDto(s.Id, s.SeatId, s.Seat.Row, s.Seat.SeatNumber, status, status == "AVAILABLE" ? null : s.HeldUntil, prices.Resolve(s, tiers));
        }).ToList());
    }
}
