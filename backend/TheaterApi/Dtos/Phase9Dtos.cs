namespace TheaterApi.Dtos;

public sealed record PointsRequest(int Amount);
public sealed record PatronSummaryDto(int Id, string FirstName, string LastName, string Email, string Preferences, int PointsBalance);
public sealed record ReviewRequest(int VenueId, string Title, string? Body, int? Rating);
public sealed record ReviewDto(int Id, int VenueId, int PersonId, string Title, string? Body, int? Rating);
