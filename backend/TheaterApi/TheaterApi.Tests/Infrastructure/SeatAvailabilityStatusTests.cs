using System.Reflection;

namespace TheaterApi.Tests.Infrastructure;

public sealed class SeatAvailabilityStatusTests
{
    [Fact]
    public void Row_3_16_ExpiredHeldSeatIsDisplayedAsAvailable()
    {
        var type = typeof(Program).Assembly.GetType("TheaterApi.Services.SeatAvailabilityStatus");
        Assert.NotNull(type);
        var method = type!.GetMethod("GetDisplayStatus", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(method);

        var now = DateTimeOffset.Parse("2030-01-01T00:00:00Z");
        var status = method!.Invoke(null, ["HELD", now.AddSeconds(-1), now]);

        Assert.Equal("AVAILABLE", status);
    }
}
