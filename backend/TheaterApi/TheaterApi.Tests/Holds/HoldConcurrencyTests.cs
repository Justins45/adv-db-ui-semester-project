using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TheaterApi.Controllers;
using TheaterApi.Dtos;
using Microsoft.Extensions.Time.Testing;
using TheaterApi.Models;
using TheaterApi.Services;
using TheaterApi.Tests.Infrastructure;

namespace TheaterApi.Tests.Holds;

public sealed class HoldConcurrencyTests(TestPostgresFixture postgres) : IClassFixture<TestPostgresFixture>
{
    [Fact]
    public async Task Row_5_16_TwentyConcurrentHoldsForOneSeat_ExactlyOneSucceeds()
    {
        var scenario = await CreateScenarioAsync(1, 20);
        var attempts = scenario.Patrons.Select(async patronId =>
        {
            await using var db = postgres.CreateDbContext();
            var service = new HoldService(db, new PriceResolutionService(), scenario.Clock);
            var controller = new HoldsController(service)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        User = new ClaimsPrincipal(new ClaimsIdentity(
                            [new Claim(ClaimTypes.NameIdentifier, patronId.ToString())], "test"))
                    }
                }
            };
            var response = await controller.Create(scenario.PerformanceId,
                new CreateHoldRequest([scenario.SeatIds[0]]), CancellationToken.None);
            return response.Result is ObjectResult result ? result.StatusCode ?? 200 : 200;
        });

        var results = await Task.WhenAll(attempts);
        Assert.Equal(1, results.Count(status => status == 200));
        Assert.Equal(19, results.Count(status => status == 409));
    }

    [Fact]
    public async Task Row_5_17_ExpiredHoldCleanup_DeletesTicketAndItemAndAbandonsCart()
    {
        var scenario = await CreateScenarioAsync(1, 1);
        int cartId;
        await using (var db = postgres.CreateDbContext())
        {
            var hold = await new HoldService(db, new PriceResolutionService(), scenario.Clock)
                .CreateAsync(scenario.Patrons[0], scenario.PerformanceId, [scenario.SeatIds[0]], CancellationToken.None);
            cartId = hold.Id;
        }
        scenario.Clock.Advance(TimeSpan.FromMinutes(11));
        await using (var db = postgres.CreateDbContext())
            Assert.Equal(1, await new HoldService(db, new PriceResolutionService(), scenario.Clock).CleanupExpiredAsync(CancellationToken.None));

        await using var verify = postgres.CreateDbContext();
        Assert.Equal("ABANDONED", await verify.Carts.Where(c => c.Id == cartId).Select(c => c.Status).SingleAsync());
        Assert.Empty(await verify.CartItems.ToListAsync());
        Assert.Empty(await verify.Tickets.ToListAsync());
        var seat = await verify.PerformanceSeats.SingleAsync(s => s.PerformanceId == scenario.PerformanceId);
        Assert.Equal("AVAILABLE", seat.Status);
        Assert.Null(seat.HeldUntil);
    }

    [Fact]
    public async Task Row_5_18_HoldingDifferentSeats_ReleasesOldHold()
    {
        var scenario = await CreateScenarioAsync(2, 1);
        int oldCartId;
        await using (var db = postgres.CreateDbContext())
        {
            var service = new HoldService(db, new PriceResolutionService(), scenario.Clock);
            oldCartId = (await service.CreateAsync(scenario.Patrons[0], scenario.PerformanceId, [scenario.SeatIds[0]], CancellationToken.None)).Id;
        }
        await using (var db = postgres.CreateDbContext())
            await new HoldService(db, new PriceResolutionService(), scenario.Clock)
                .CreateAsync(scenario.Patrons[0], scenario.PerformanceId, [scenario.SeatIds[1]], CancellationToken.None);

        await using var verify = postgres.CreateDbContext();
        Assert.Equal("ABANDONED", await verify.Carts.Where(c => c.Id == oldCartId).Select(c => c.Status).SingleAsync());
        Assert.Equal(1, await verify.Carts.CountAsync(c => c.PatronId == scenario.Patrons[0] && c.Status == "ACTIVE"));
        Assert.Equal("AVAILABLE", await verify.PerformanceSeats.Where(s => s.SeatId == scenario.SeatIds[0]).Select(s => s.Status).SingleAsync());
        Assert.Equal("HELD", await verify.PerformanceSeats.Where(s => s.SeatId == scenario.SeatIds[1]).Select(s => s.Status).SingleAsync());
    }

    [Fact]
    public async Task Row_5_19_NonScheduledPerformance_IsRejected()
    {
        var scenario = await CreateScenarioAsync(1, 1);
        await using (var db = postgres.CreateDbContext())
        {
            var performance = await db.Performances.SingleAsync(p => p.Id == scenario.PerformanceId);
            performance.Status = "CANCELLED";
            await db.SaveChangesAsync();
        }
        await using var attempt = postgres.CreateDbContext();
        await Assert.ThrowsAsync<HoldConflictException>(() =>
            new HoldService(attempt, new PriceResolutionService(), scenario.Clock)
                .CreateAsync(scenario.Patrons[0], scenario.PerformanceId, [scenario.SeatIds[0]], CancellationToken.None));
    }

    [Fact]
    public async Task ExpiredHoldTakeover_RemovesStaleTicketAndCartItem()
    {
        var scenario = await CreateScenarioAsync(1, 2);
        int expiredCartId;
        await using (var db = postgres.CreateDbContext())
        {
            expiredCartId = (await new HoldService(db, new PriceResolutionService(), scenario.Clock)
                .CreateAsync(scenario.Patrons[0], scenario.PerformanceId, [scenario.SeatIds[0]], CancellationToken.None)).Id;
        }
        scenario.Clock.Advance(TimeSpan.FromMinutes(11));
        await using (var db = postgres.CreateDbContext())
            await new HoldService(db, new PriceResolutionService(), scenario.Clock)
                .CreateAsync(scenario.Patrons[1], scenario.PerformanceId, [scenario.SeatIds[0]], CancellationToken.None);

        await using var verify = postgres.CreateDbContext();
        Assert.Equal("ABANDONED", await verify.Carts.Where(c => c.Id == expiredCartId).Select(c => c.Status).SingleAsync());
        Assert.Equal(1, await verify.CartItems.CountAsync());
        Assert.Equal(1, await verify.Tickets.CountAsync());
        Assert.Equal("HELD", await verify.PerformanceSeats.Where(s => s.SeatId == scenario.SeatIds[0]).Select(s => s.Status).SingleAsync());
    }

    private async Task<Scenario> CreateScenarioAsync(int seatCount, int patronCount)
    {
        await postgres.ResetAsync();
        var clock = new FakeTimeProvider(DateTimeOffset.Parse("2026-01-01T00:00:00Z"));
        await using var db = postgres.CreateDbContext();
        db.Roles.Add(new Role { RoleName = "PATRON" });
        await db.SaveChangesAsync();
        var data = new TestDataFactory(db, clock);
        var performance = await data.CreatePerformanceAsync(seatCount, 42m);
        var seatIds = await db.PerformanceSeats.Where(s => s.PerformanceId == performance.Id).Select(s => s.SeatId).ToArrayAsync();
        var patrons = new int[patronCount];
        for (var i = 0; i < patrons.Length; i++)
            patrons[i] = (await data.CreatePatronAsync($"patron{i}-{Guid.NewGuid():N}@example.test")).Person.Id;
        return new Scenario(performance.Id, seatIds, patrons, clock);
    }

    private sealed record Scenario(int PerformanceId, int[] SeatIds, int[] Patrons, FakeTimeProvider Clock);
}
