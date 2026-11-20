using Microsoft.EntityFrameworkCore;
using TheaterApi.Data;
using TheaterApi.Dtos;
using TheaterApi.Models;
using TheaterApi.Logging;

namespace TheaterApi.Services;

public sealed class CheckoutConflictException(string message) : Exception(message);
public sealed class PaymentDeclinedException() : Exception;

public sealed class OrderService(AppDbContext db, TimeProvider clock, ILogSink logs, IPaymentService paymentService)
{
    private static readonly string[] Methods = ["CREDIT_CARD", "DEBIT_CARD", "PAYPAL", "APPLE_PAY", "GOOGLE_PAY", "MASTERCARD", "VISA"];
    public async Task<OrderDto> CheckoutAsync(int patronId, CheckoutRequest request, string? idempotencyKey, CancellationToken ct)
    {
        var method = request.Method.Trim().ToUpperInvariant();
        if (!Methods.Contains(method)) throw new CheckoutConflictException("Payment method is not supported.");
        var paymentResult = await paymentService.ChargeAsync(request.CardNumber, ct);
        if (!paymentResult.Approved) throw new PaymentDeclinedException();
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            var existing = await db.Orders.AsNoTracking().Include(o => o.Items).SingleOrDefaultAsync(o => o.Cart.PatronId == patronId && o.IdempotencyKey == idempotencyKey, ct);
            if (existing is not null) { await tx.CommitAsync(ct); return ToDto(existing); }
        }
        var now = clock.GetUtcNow();
        var cart = await db.Carts.Include(c => c.Items).ThenInclude(i => i.Ticket).ThenInclude(t => t.PerformanceSeat).SingleOrDefaultAsync(c => c.PatronId == patronId && c.Status == "ACTIVE", ct);
        if (cart is null || cart.Items.Count == 0) throw new CheckoutConflictException("There is no active hold.");
        if (cart.Items.Any(i => i.Ticket.PerformanceSeat.Status != "HELD" || i.Ticket.PerformanceSeat.HeldUntil <= now)) throw new CheckoutConflictException("The hold has expired.");
        var promo = await ResolvePromoAsync(request.PromoCode, now, ct);
        var subtotal = cart.Items.Sum(i => i.Ticket.Price);
        var discount = promo?.DOff ?? (promo?.POff is decimal fraction ? subtotal * fraction : 0m);
        var total = Math.Max(0m, subtotal - discount).RoundMoney();
        var performanceId = cart.Items.First().Ticket.PerformanceSeat.PerformanceId;
        var order = new Order { CartId = cart.Id, PromoCodeId = promo?.Id, DateTime = now, Status = "PAID", TotalAmount = total, PaymentReference = paymentResult.Reference, IdempotencyKey = string.IsNullOrWhiteSpace(idempotencyKey) ? null : idempotencyKey };
        db.Orders.Add(order);
        foreach (var item in cart.Items) { item.Ticket.PerformanceSeat.Status = "SOLD"; item.Ticket.PerformanceSeat.HeldUntil = null; order.Items.Add(new OrderItem { PerformanceSeatId = item.Ticket.PerformanceSeatId, PriceAtPurchase = item.Ticket.Price }); }
        db.Payments.Add(new Payment { Order = order, Method = method, Amount = total, DateTime = now }); cart.Status = "CHECKED_OUT";
        await db.SaveChangesAsync(ct);
        var remaining = await db.PerformanceSeats.AnyAsync(s => s.PerformanceId == performanceId && (s.Status == "AVAILABLE" || s.Status == "HELD") && (s.Status == "AVAILABLE" || s.HeldUntil > now), ct);
        if (!remaining) { var performance = await db.Performances.SingleAsync(p => p.Id == performanceId, ct); performance.Filled = true; await db.SaveChangesAsync(ct); await logs.PerformanceStatusAsync(performanceId, null, performance.Status, "SOLD_OUT"); }
        await tx.CommitAsync(ct); return ToDto(order);
    }

    public async Task<IReadOnlyList<OrderDto>> ListAsync(int patronId, CancellationToken ct) { var orders = await db.Orders.AsNoTracking().Include(o => o.Items).Where(o => o.Cart.PatronId == patronId).OrderByDescending(o => o.DateTime).ToListAsync(ct); return orders.Select(ToDto).ToList(); }
    public async Task<OrderDto?> GetAsync(int patronId, int id, CancellationToken ct) { var o = await db.Orders.AsNoTracking().Include(x => x.Items).SingleOrDefaultAsync(x => x.Id == id && x.Cart.PatronId == patronId, ct); return o is null ? null : ToDto(o); }
    private async Task<PromoCode?> ResolvePromoAsync(string? code, DateTimeOffset now, CancellationToken ct)
    { if (string.IsNullOrWhiteSpace(code)) return null; var p = await db.PromoCodes.SingleOrDefaultAsync(x => x.Code.ToLower() == code.Trim().ToLower(), ct); if (p is null || !p.Active || p.ExpiryDate <= now || (p.DOff is null && p.POff is null)) throw new CheckoutConflictException("Promo code is invalid, inactive, expired, or empty."); return p; }
    private static OrderDto ToDto(Order o) => new(o.Id, o.Status, o.TotalAmount, o.DateTime, o.PaymentReference, o.Items.Select(i => new OrderItemDto(i.PerformanceSeatId, i.PriceAtPurchase)).ToList());
}
internal static class DecimalExtensions { public static decimal RoundMoney(this decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero); }
