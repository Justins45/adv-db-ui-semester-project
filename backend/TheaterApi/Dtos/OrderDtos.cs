using System.ComponentModel.DataAnnotations;

namespace TheaterApi.Dtos;
public sealed record CheckoutRequest([Required] string Method, string? CardNumber, string? PromoCode);
public sealed record OrderDto(int Id, string Status, decimal TotalAmount, DateTimeOffset DateTime, string? PaymentReference, IReadOnlyList<OrderItemDto> Items);
public sealed record OrderItemDto(int PerformanceSeatId, decimal PriceAtPurchase);
public sealed record PromoCodeDto(int Id, string Code, decimal? DOff, decimal? POff, bool Active, DateTimeOffset? ExpiryDate);
public sealed record PromoCodeRequest([Required] string Code, [Range(0, double.MaxValue)] decimal? DOff, [Range(0, 1)] decimal? POff, bool Active = true, DateTimeOffset? ExpiryDate = null);
