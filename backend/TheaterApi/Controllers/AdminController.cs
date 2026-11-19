using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TheaterApi.Data;
using TheaterApi.Dtos;
using TheaterApi.Models;

namespace TheaterApi.Controllers;

[ApiController, Authorize(Roles = "ADMIN"), Route("admin")]
public sealed class AdminController(AppDbContext db, TimeProvider clock) : ControllerBase
{
    [HttpPost("events")]
    public async Task<IActionResult> CreateEvent(AdminEventRequest r, CancellationToken ct) { var e = new Event { Title = r.Title.Trim(), Description = r.Description, Status = r.Status, LiveDate = r.LiveDate, Image = r.Image }; await AddGenres(e, r.GenreIds, ct); db.Events.Add(e); await db.SaveChangesAsync(ct); return Created($"/events/{e.Id}", new { e.Id }); }
    [HttpPut("events/{id:int}")]
    public async Task<IActionResult> UpdateEvent(int id, AdminEventRequest r, CancellationToken ct) { var e = await db.Events.Include(x => x.Genres).SingleOrDefaultAsync(x => x.Id == id, ct); if (e is null) return NotFound(); e.Title = r.Title.Trim(); e.Description = r.Description; e.Status = r.Status; e.LiveDate = r.LiveDate; e.Image = r.Image; e.Genres.Clear(); await AddGenres(e, r.GenreIds, ct); await db.SaveChangesAsync(ct); return NoContent(); }
    [HttpDelete("events/{id:int}")] public Task<IActionResult> DeleteEvent(int id, CancellationToken ct) => Delete(db.Events, id, ct);

    [HttpPost("genres")] public async Task<IActionResult> CreateGenre(AdminGenreRequest r, CancellationToken ct) { var g = new Genre { Name = r.Name.Trim(), Active = r.Active }; db.Genres.Add(g); await db.SaveChangesAsync(ct); return Created($"/genres/{g.Id}", new { g.Id, g.Name, g.Active }); }
    [HttpPut("genres/{id:int}")] public async Task<IActionResult> UpdateGenre(int id, AdminGenreRequest r, CancellationToken ct) { var g = await db.Genres.FindAsync([id], ct); if (g is null) return NotFound(); g.Name = r.Name.Trim(); g.Active = r.Active; await db.SaveChangesAsync(ct); return NoContent(); }
    [HttpDelete("genres/{id:int}")] public Task<IActionResult> DeleteGenre(int id, CancellationToken ct) => Delete(db.Genres, id, ct);

    [HttpPost("performers")]
    public async Task<IActionResult> CreatePerformer(AdminPersonRequest r, CancellationToken ct) { if (await db.People.AnyAsync(p => p.Email == r.Email.Trim().ToLower() || p.Phone == r.Phone.Trim(), ct)) return Conflict(); var patronRole = await db.Roles.SingleAsync(x => x.RoleName == "PATRON", ct); var p = new Person { FirstName = r.FirstName.Trim(), LastName = r.LastName.Trim(), Email = r.Email.Trim().ToLowerInvariant(), Phone = r.Phone.Trim() }; p.Password = new PasswordHasher<Person>().HashPassword(p, Convert.ToHexString(Guid.NewGuid().ToByteArray())); p.Patron = new Patron { RoleId = patronRole.Id }; p.Performer = new Performer { Role = r.PerformerRole }; db.People.Add(p); await db.SaveChangesAsync(ct); return Created($"/admin/performers/{p.Id}", new { p.Id }); }
    [HttpDelete("performers/{id:int}")] public async Task<IActionResult> DeletePerformer(int id, CancellationToken ct) { var p = await db.Performers.FindAsync([id], ct); if (p is null) return NotFound(); db.Performers.Remove(p); await db.SaveChangesAsync(ct); return NoContent(); }

    [HttpPost("venues")] public async Task<IActionResult> CreateVenue(AdminVenueRequest r, CancellationToken ct) { var v = new Venue { Name=r.Name, Address=r.Address, City=r.City, Province=r.Province, PostalCode=r.PostalCode }; db.Venues.Add(v); await db.SaveChangesAsync(ct); return Created($"/admin/venues/{v.Id}", new { v.Id }); }
    [HttpPost("stages")] public async Task<IActionResult> CreateStage(AdminStageRequest r, CancellationToken ct) { if (!await db.Venues.AnyAsync(x=>x.Id==r.VenueId,ct)) return BadRequest("Venue does not exist."); var s=new Stage { VenueId=r.VenueId, Name=r.Name }; db.Stages.Add(s); await db.SaveChangesAsync(ct); return Created($"/admin/stages/{s.Id}", new { s.Id }); }
    [HttpPost("sections")] public async Task<IActionResult> CreateSection(AdminSectionRequest r, CancellationToken ct) { if (!await db.Stages.AnyAsync(x=>x.Id==r.StageId,ct)) return BadRequest("Stage does not exist."); var s=new Section { StageId=r.StageId, Name=r.Name }; db.Sections.Add(s); await db.SaveChangesAsync(ct); return Created($"/admin/sections/{s.Id}", new { s.Id }); }
    [HttpPost("sections/{id:int}/seats")]
    public async Task<IActionResult> AddSeats(int id, AdminSeatsRequest r, CancellationToken ct) { if (!await db.Sections.AnyAsync(x=>x.Id==id,ct)) return NotFound(); var seats=r.Seats.Select(x=>new Seat { SectionId=id, Row=x.Row, SeatNumber=x.SeatNumber }).ToList(); db.Seats.AddRange(seats); await db.SaveChangesAsync(ct); return Created($"/admin/sections/{id}/seats", seats.Select(x=>new { x.Id, x.Row, x.SeatNumber })); }

    [HttpPost("performances")]
    public async Task<IActionResult> CreatePerformance(AdminPerformanceRequest r, CancellationToken ct)
    {
        var stage = await db.Stages.Include(x=>x.Sections).ThenInclude(x=>x.Seats).SingleOrDefaultAsync(x=>x.Id==r.StageId,ct); if (stage is null || !await db.Events.AnyAsync(x=>x.Id==r.EventId,ct)) return BadRequest("Event or stage does not exist.");
        if (r.PricesBySection.Keys.Any(id=>stage.Sections.All(s=>s.Id!=id)) || r.PricesBySection.Keys.Any(id=>!stage.Sections.Any(s=>s.Id==id))) return BadRequest("Every price section must belong to the performance stage.");
        if (r.PricesBySection.Keys.Any(id => !stage.Sections.Any(s=>s.Id==id))) return BadRequest("Section does not belong to stage.");
        await using var tx=await db.Database.BeginTransactionAsync(ct); var p=new Performance { EventId=r.EventId, StageId=r.StageId, DateTime=r.DateTime, Status=r.Status }; db.Performances.Add(p); await db.SaveChangesAsync(ct);
        foreach(var section in stage.Sections) { if(!r.PricesBySection.TryGetValue(section.Id,out var price)) return BadRequest("A price is required for every stage section."); db.PriceTiers.Add(new PriceTier { PerformanceId=p.Id, SectionId=section.Id, Price=price }); foreach(var seat in section.Seats) db.PerformanceSeats.Add(new PerformanceSeat { PerformanceId=p.Id, SeatId=seat.Id, Status="AVAILABLE" }); }
        if (r.PerformerIds is not null) { var cast=await db.Performers.Where(x=>r.PerformerIds.Contains(x.Id)).ToListAsync(ct); if(cast.Count!=r.PerformerIds.Distinct().Count()) return BadRequest("Performer does not exist."); foreach(var x in cast) p.Performers.Add(x); }
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return Created($"/performances/{p.Id}", new { p.Id });
    }

    [HttpPut("people/{id:int}/role")]
    public async Task<IActionResult> ChangeRole(int id, AdminRoleRequest r, CancellationToken ct) { var person=await db.People.Include(x=>x.Patron).Include(x=>x.Staff).SingleOrDefaultAsync(x=>x.Id==id,ct); if(person is null || r.Role is not ("PATRON" or "STAFF" or "ADMIN")) return BadRequest("Invalid person or role."); var roles=await db.Roles.ToDictionaryAsync(x=>x.RoleName,x=>x.Id,ct); await using var tx=await db.Database.BeginTransactionAsync(ct); if(person.Patron is null) person.Patron=new Patron { RoleId=roles["PATRON"] }; if(r.Role=="PATRON") { if(person.Staff is not null) db.Staff.Remove(person.Staff); } else { if(r.AssignedVenue is null || !await db.Venues.AnyAsync(x=>x.Id==r.AssignedVenue,ct)) return BadRequest("assignedVenue is required and must exist."); if(person.Staff is null) person.Staff=new Staff { RoleId=roles[r.Role], AssignedVenue=r.AssignedVenue.Value, HireDate=clock.GetUtcNow() }; else { person.Staff.RoleId=roles[r.Role]; person.Staff.AssignedVenue=r.AssignedVenue.Value; } } await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return NoContent(); }

    private async Task AddGenres(Event e, IReadOnlyList<int>? ids, CancellationToken ct) { if(ids is null) return; var genres=await db.Genres.Where(x=>ids.Contains(x.Id)).ToListAsync(ct); if(genres.Count!=ids.Distinct().Count()) throw new InvalidOperationException("Genre does not exist."); foreach(var g in genres)e.Genres.Add(g); }
    private async Task<IActionResult> Delete<TEntity>(DbSet<TEntity> set,int id,CancellationToken ct) where TEntity:class { var x=await set.FindAsync([id],ct); if(x is null)return new NotFoundResult(); set.Remove(x); await db.SaveChangesAsync(ct); return new NoContentResult(); }
}
