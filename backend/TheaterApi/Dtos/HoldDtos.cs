using System.ComponentModel.DataAnnotations;

namespace TheaterApi.Dtos;
public sealed record CreateHoldRequest([Required, MinLength(1), MaxLength(8)] IReadOnlyList<int> SeatIds);
public sealed record HoldSeatDto(int PerformanceSeatId, int SeatId, string Row, int SeatNumber, decimal Price, DateTimeOffset HeldUntil);
public sealed record HoldDto(int Id, int PerformanceId, DateTimeOffset HeldUntil, IReadOnlyList<HoldSeatDto> Seats);
