using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TheaterApi.Data;
using TheaterApi.Dtos;
using TheaterApi.Models;

namespace TheaterApi.Controllers;
[ApiController, Authorize(Roles = "STAFF,ADMIN"), Route("api/admin/promo-codes")]
public sealed class PromoCodesController(AppDbContext db) : ControllerBase
{
    [HttpGet] public async Task<ActionResult<IReadOnlyList<PromoCodeDto>>> List(CancellationToken ct) => Ok(await db.PromoCodes.AsNoTracking().OrderBy(p => p.Code).Select(p => new PromoCodeDto(p.Id, p.Code, p.DOff, p.POff, p.Active, p.ExpiryDate)).ToListAsync(ct));
    [HttpPost] public async Task<ActionResult<PromoCodeDto>> Create(PromoCodeRequest request, CancellationToken ct) { if (request.DOff is null == (request.POff is null)) return BadRequest("Provide exactly one discount."); var p = new PromoCode { Code = request.Code.Trim(), DOff = request.DOff, POff = request.POff, Active = request.Active, ExpiryDate = request.ExpiryDate }; db.PromoCodes.Add(p); await db.SaveChangesAsync(ct); return CreatedAtAction(nameof(List), new { id = p.Id }, new PromoCodeDto(p.Id, p.Code, p.DOff, p.POff, p.Active, p.ExpiryDate)); }
    [HttpPut("{id:int}")] public async Task<IActionResult> Update(int id, PromoCodeRequest request, CancellationToken ct) { if (request.DOff is null == (request.POff is null)) return BadRequest("Provide exactly one discount."); var p = await db.PromoCodes.FindAsync([id], ct); if (p is null) return NotFound(); p.Code = request.Code.Trim(); p.DOff = request.DOff; p.POff = request.POff; p.Active = request.Active; p.ExpiryDate = request.ExpiryDate; await db.SaveChangesAsync(ct); return NoContent(); }
    [HttpDelete("{id:int}")] public async Task<IActionResult> Delete(int id, CancellationToken ct) { var p = await db.PromoCodes.FindAsync([id], ct); if (p is null) return NotFound(); db.PromoCodes.Remove(p); await db.SaveChangesAsync(ct); return NoContent(); }
}
