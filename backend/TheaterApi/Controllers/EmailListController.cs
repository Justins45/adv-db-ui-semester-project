using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TheaterApi.Data;
namespace TheaterApi.Controllers;
[ApiController, Authorize, Route("api/account/email-list")]
public sealed class EmailListController(AppDbContext db, TimeProvider clock) : ControllerBase
{
    /// <summary>Handles the POST request for this resource.</summary>
    [HttpPost] public async Task<IActionResult> Join(CancellationToken ct) { var id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!); if (!await db.EmailLists.AnyAsync(x => x.PersonId == id, ct)) { db.EmailLists.Add(new Models.EmailList { PersonId = id, Added = clock.GetUtcNow() }); await db.SaveChangesAsync(ct); } return NoContent(); }
    /// <summary>Handles the DELETE request for this resource.</summary>
    [HttpDelete] public async Task<IActionResult> Leave(CancellationToken ct) { var id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!); var row = await db.EmailLists.FindAsync([id], ct); if (row is not null) { db.EmailLists.Remove(row); await db.SaveChangesAsync(ct); } return NoContent(); }
}
