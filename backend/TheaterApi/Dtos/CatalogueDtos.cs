namespace TheaterApi.Dtos;

public sealed record EventListDto(int Id, string Title, string? Description, string Status, DateTimeOffset? LiveDate, IReadOnlyList<string> Genres);
public sealed record EventDetailDto(int Id, string Title, string? Description, string Status, DateTimeOffset? LiveDate, IReadOnlyList<string> Genres, IReadOnlyList<PerformanceSummaryDto> Performances);
public sealed record PerformanceSummaryDto(int Id, DateTimeOffset DateTime, string Status, int StageId, bool Filled);
public sealed record PerformanceDetailDto(int Id, int EventId, int StageId, DateTimeOffset DateTime, string Status, bool Filled);
public sealed record PerformanceSeatDto(int Id, int SeatId, string Row, int SeatNumber, string Status, DateTimeOffset? HeldUntil, decimal Price);
public sealed record GenreDto(int Id, string Name, bool Active);
