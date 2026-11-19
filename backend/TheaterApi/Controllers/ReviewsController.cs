using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TheaterApi.Data;
using TheaterApi.Dtos;
using TheaterApi.Models;

namespace TheaterApi.Controllers;

[ApiController, Route("api/reviews")]
public sealed class ReviewsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ReviewDto>>> List(CancellationToken ct) => Ok(await db.Reviews.AsNoTracking().OrderByDescending(x => x.Id).Select(x => new ReviewDto(x.Id, x.VenueId, x.PersonId, x.Title, x.Body, x.Rating)).ToListAsync(ct));
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ReviewDto>> Get(int id, CancellationToken ct) { var review = await db.Reviews.AsNoTracking().Where(x => x.Id == id).Select(x => new ReviewDto(x.Id, x.VenueId, x.PersonId, x.Title, x.Body, x.Rating)).SingleOrDefaultAsync(ct); return review is null ? NotFound() : Ok(review); }
    [Authorize(Roles = "PATRON,STAFF,ADMIN"), HttpPost]
    public async Task<ActionResult<ReviewDto>> Create(ReviewRequest request, CancellationToken ct) { if (request.Rating is < 1 or > 5 || !await db.Venues.AnyAsync(x => x.Id == request.VenueId, ct)) return BadRequest(); var review = new Review { VenueId = request.VenueId, PersonId = PersonId(), Title = request.Title.Trim(), Body = request.Body, Rating = request.Rating }; db.Reviews.Add(review); await db.SaveChangesAsync(ct); return CreatedAtAction(nameof(Get), new { id = review.Id }, ToDto(review)); }
    [Authorize(Roles = "PATRON,STAFF,ADMIN"), HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, ReviewRequest request, CancellationToken ct) { var review = await db.Reviews.FindAsync([id], ct); if (review is null) return NotFound(); if (review.PersonId != PersonId()) return Forbid(); if (request.Rating is < 1 or > 5 || !await db.Venues.AnyAsync(x => x.Id == request.VenueId, ct)) return BadRequest(); review.VenueId = request.VenueId; review.Title = request.Title.Trim(); review.Body = request.Body; review.Rating = request.Rating; review.Embedding = null; await db.SaveChangesAsync(ct); return Ok(ToDto(review)); }
    [Authorize(Roles = "PATRON,STAFF,ADMIN"), HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct) { var review = await db.Reviews.FindAsync([id], ct); if (review is null) return NotFound(); if (review.PersonId != PersonId()) return Forbid(); db.Reviews.Remove(review); await db.SaveChangesAsync(ct); return NoContent(); }
    private int PersonId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private static ReviewDto ToDto(Review x) => new(x.Id, x.VenueId, x.PersonId, x.Title, x.Body, x.Rating);
}
