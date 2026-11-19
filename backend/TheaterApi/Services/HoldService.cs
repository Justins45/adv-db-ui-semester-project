using Microsoft.EntityFrameworkCore;
using TheaterApi.Data;
using TheaterApi.Dtos;
using TheaterApi.Models;

namespace TheaterApi.Services;

public sealed class HoldConflictException(string message) : Exception(message);

public sealed class HoldService(AppDbContext db, IPriceResolutionService pricing, TimeProvider clock)
{
    private static readonly TimeSpan HoldDuration = TimeSpan.FromMinutes(10);

    public async Task<HoldDto> CreateAsync(int patronId, int performanceId, IReadOnlyList<int> seatIds, CancellationToken ct)
    {
        if (seatIds.Count == 0 || seatIds.Count > 8 || seatIds.Distinct().Count() != seatIds.Count) throw new HoldConflictException("A hold must contain between 1 and 8 distinct seats.");
        var now = clock.GetUtcNow(); var until = now.Add(HoldDuration);
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        var performance = await db.Performances.AsNoTracking().SingleOrDefaultAsync(p => p.Id == performanceId, ct);
        if (performance is null) throw new KeyNotFoundException("Performance was not found.");
        if (performance.Status != "SCHEDULED" || performance.DateTime <= now) throw new HoldConflictException("Seats can only be held for a future scheduled performance.");

        var oldCart = await db.Carts.Include(c => c.Items).ThenInclude(i => i.Ticket).SingleOrDefaultAsync(c => c.PatronId == patronId && c.Status == "ACTIVE", ct);
        if (oldCart is not null)
        {
            var oldSeatIds = oldCart.Items.Select(i => i.Ticket.PerformanceSeatId).ToList();
            foreach (var oldSeatId in oldSeatIds) await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE performance_seat SET status = 'AVAILABLE', held_until = NULL WHERE id = {oldSeatId} AND status = 'HELD'", ct);
            db.CartItems.RemoveRange(oldCart.Items); oldCart.Status = "ABANDONED"; await db.SaveChangesAsync(ct);
        }

        var cart = new Cart { PatronId = patronId, Status = "ACTIVE" }; db.Carts.Add(cart); await db.SaveChangesAsync(ct);
        var seats = await db.PerformanceSeats.Include(s => s.Performance).Include(s => s.Seat).ThenInclude(s => s.Section).Where(s => s.PerformanceId == performanceId && seatIds.Contains(s.SeatId)).ToListAsync(ct);
        if (seats.Count != seatIds.Count) throw new HoldConflictException("One or more seats do not belong to the performance.");
        var tiers = await db.PriceTiers.AsNoTracking().Where(t => !t.PerformanceId.HasValue || t.PerformanceId == performanceId).ToListAsync(ct);
        var held = new List<(PerformanceSeat Seat, decimal Price)>();
        foreach (var seat in seats)
        {
            var changed = await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE performance_seat SET status = 'HELD', held_until = {until} WHERE id = {seat.Id} AND performance_id = {performanceId} AND status IN ('AVAILABLE', 'HELD') AND (status = 'AVAILABLE' OR held_until IS NULL OR held_until <= {now})", ct);
            if (changed != 1) throw new HoldConflictException("One or more seats are already held or sold.");
            held.Add((seat, pricing.Resolve(seat, tiers)));
        }
        foreach (var (seat, price) in held)
        {
            var ticket = new Ticket { PerformanceSeatId = seat.Id, DateTime = performance.DateTime, Price = price };
            var item = new CartItem { Cart = cart, Ticket = ticket }; db.CartItems.Add(item);
        }
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        return await GetCartAsync(patronId, ct) ?? throw new InvalidOperationException("Hold was not created.");
    }

    public Task<HoldDto?> CurrentAsync(int patronId, CancellationToken ct) => GetCartAsync(patronId, ct);

    public async Task<bool> DeleteAsync(int patronId, int cartId, CancellationToken ct)
    {
        var cart = await db.Carts.Include(c => c.Items).ThenInclude(i => i.Ticket).SingleOrDefaultAsync(c => c.Id == cartId && c.PatronId == patronId && c.Status == "ACTIVE", ct);
        if (cart is null) return false;
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        foreach (var item in cart.Items) await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE performance_seat SET status = 'AVAILABLE', held_until = NULL WHERE id = {item.Ticket.PerformanceSeatId} AND status = 'HELD'", ct);
        db.CartItems.RemoveRange(cart.Items); cart.Status = "ABANDONED"; await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return true;
    }

    public async Task<int> CleanupExpiredAsync(CancellationToken ct)
    {
        var now = clock.GetUtcNow();
        var carts = await db.Carts.Include(c => c.Items).ThenInclude(i => i.Ticket).Where(c => c.Status == "ACTIVE" && c.Items.Any(i => i.Ticket.PerformanceSeat.Status == "HELD" && i.Ticket.PerformanceSeat.HeldUntil <= now)).ToListAsync(ct);
        var count = 0;
        foreach (var cart in carts) { foreach (var item in cart.Items) await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE performance_seat SET status = 'AVAILABLE', held_until = NULL WHERE id = {item.Ticket.PerformanceSeatId} AND status = 'HELD' AND held_until <= {now}", ct); db.CartItems.RemoveRange(cart.Items); cart.Status = "ABANDONED"; count++; }
        if (count > 0) await db.SaveChangesAsync(ct); return count;
    }

    private async Task<HoldDto?> GetCartAsync(int patronId, CancellationToken ct)
    {
        var now = clock.GetUtcNow(); await CleanupExpiredAsync(ct);
        var cart = await db.Carts.AsNoTracking().Include(c => c.Items).ThenInclude(i => i.Ticket).ThenInclude(t => t.PerformanceSeat).ThenInclude(s => s.Seat).SingleOrDefaultAsync(c => c.PatronId == patronId && c.Status == "ACTIVE", ct);
        if (cart is null || cart.Items.Count == 0) return null;
        var first = cart.Items.First().Ticket.PerformanceSeat;
        return new HoldDto(cart.Id, first.PerformanceId, cart.Items.Min(i => i.Ticket.PerformanceSeat.HeldUntil)!.Value, cart.Items.Select(i => { var s = i.Ticket.PerformanceSeat; return new HoldSeatDto(s.Id, s.SeatId, s.Seat.Row, s.Seat.SeatNumber, i.Ticket.Price, s.HeldUntil!.Value); }).ToList());
    }
}
