using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TheaterApi.Dtos;
using TheaterApi.Services;

namespace TheaterApi.Controllers;
[ApiController, Authorize(Roles = "PATRON,STAFF,ADMIN"), Route("api/orders")]
public sealed class OrdersController(OrderService orders) : ControllerBase
{
    /// <summary>Handles the POST request for this resource.</summary>
    [HttpPost] public async Task<ActionResult<OrderDto>> Checkout(CheckoutRequest request, [FromHeader(Name = "Idempotency-Key")] string? key, CancellationToken ct) { try { return Ok(await orders.CheckoutAsync(Id(), request, key, ct)); } catch (PaymentDeclinedException) { return StatusCode(402, new ProblemDetails { Title = "Payment declined", Status = 402 }); } catch (DbUpdateConcurrencyException) { return Conflict(new ProblemDetails { Title = "Checkout conflict", Status = 409 }); } catch (CheckoutConflictException e) { return BadRequest(new ProblemDetails { Title = "Checkout failed", Detail = e.Message, Status = 400 }); } }
    /// <summary>Handles the GET request for this resource.</summary>
    [HttpGet] public async Task<ActionResult<IReadOnlyList<OrderDto>>> List(CancellationToken ct) => Ok(await orders.ListAsync(Id(), ct));
    /// <summary>Handles the GET request for {id:int}.</summary>
    [HttpGet("{id:int}")] public async Task<ActionResult<OrderDto>> Get(int id, CancellationToken ct) => (await orders.GetAsync(Id(), id, ct)) is { } order ? Ok(order) : NotFound();
    private int Id() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
