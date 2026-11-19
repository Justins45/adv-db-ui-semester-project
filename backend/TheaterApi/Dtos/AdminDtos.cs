namespace TheaterApi.Dtos;

public sealed record AdminEventRequest(string Title, string? Description, string Status, DateTimeOffset? LiveDate, byte[]? Image, IReadOnlyList<int>? GenreIds);
public sealed record AdminGenreRequest(string Name, bool Active = true);
public sealed record AdminPersonRequest(string FirstName, string LastName, string Email, string Phone, string? PerformerRole);
public sealed record AdminVenueRequest(string Name, string Address, string City, string Province, string PostalCode);
public sealed record AdminStageRequest(int VenueId, string Name);
public sealed record AdminSectionRequest(int StageId, string Name);
public sealed record AdminSeatRequest(string Row, int SeatNumber);
public sealed record AdminSeatsRequest(IReadOnlyList<AdminSeatRequest> Seats);
public sealed record AdminPerformanceRequest(int EventId, int StageId, DateTimeOffset DateTime, string Status, IReadOnlyDictionary<int, decimal> PricesBySection, IReadOnlyList<int>? PerformerIds);
public sealed record AdminRoleRequest(string Role, int? AssignedVenue);
