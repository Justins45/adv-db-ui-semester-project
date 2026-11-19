using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TheaterApi.Data;
using TheaterApi.Dtos;
using TheaterApi.Services;

namespace TheaterApi.Controllers;
[ApiController, Route("api/auth")]
public sealed class AuthController(AuthService auth, AppDbContext db) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request, CancellationToken ct) { try { var p = await auth.RegisterAsync(request.FirstName, request.LastName, request.Email, request.Phone, request.Password, ct); return Created("auth/me", new { p.Id, p.Email }); } catch (InvalidOperationException e) { return Conflict(new ProblemDetails { Title = "Registration failed", Detail = e.Message, Status = 409 }); } }
    [HttpPost("login"), EnableRateLimiting("auth")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken ct) { var result = await auth.LoginAsync(request.Email, request.Password, HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown", ct); return result.Person is null ? Unauthorized() : Ok(new AuthResponse(result.Person.Id, result.Person.Email, auth.Role(result.Person), result.Token!)); }
    [Authorize, HttpGet("me")]
    public async Task<ActionResult<object>> Me(CancellationToken ct) { var id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!); var p = await db.People.Include(x => x.Patron).ThenInclude(x => x!.Role).Include(x => x.Staff).ThenInclude(x => x!.Role).SingleOrDefaultAsync(x => x.Id == id, ct); return p is null ? NotFound() : Ok(new { p.Id, p.FirstName, p.LastName, p.Email, Role = auth.Role(p) }); }
    [HttpPost("password-reset/request"), EnableRateLimiting("auth")] public async Task<IActionResult> ResetRequest(ResetRequest request, CancellationToken ct) { await auth.RequestResetAsync(request.Email, ct); return Accepted(new { message = "If the account exists, reset instructions were sent." }); }
    [HttpPost("password-reset/confirm"), EnableRateLimiting("auth")] public async Task<IActionResult> ResetConfirm(ResetConfirmRequest request, CancellationToken ct) => await auth.ConfirmResetAsync(request.Email, request.Token, request.NewPassword, ct) ? NoContent() : BadRequest(new ProblemDetails { Title = "Invalid or expired reset token", Status = 400 });
}
