namespace TheaterApi.Services;

public static class SeatAvailabilityStatus
{
    public static string GetDisplayStatus(string status, DateTimeOffset? heldUntil, DateTimeOffset now) =>
        status == "HELD" && heldUntil <= now ? "AVAILABLE" : status;
}
