using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;
using TheaterApi.Controllers;
using TheaterApi.Dtos;
using TheaterApi.Logging;
using TheaterApi.Models;
using TheaterApi.Services;
using TheaterApi.Tests.Infrastructure;

namespace TheaterApi.Tests.Orders;

public sealed class CheckoutConcurrencyTests(TestPostgresFixture postgres) : IClassFixture<TestPostgresFixture>
{
    [Fact]
    public async Task Row_6_3_TwoConcurrentCheckoutsOfSameCart_ExactlyOneSucceeds()
    {
        await postgres.ResetAsync();
        var clock = new FakeTimeProvider(DateTimeOffset.Parse("2026-01-01T00:00:00Z"));
        int patronId;
        int performanceId;
        int seatId;
        await using (var db = postgres.CreateDbContext())
        {
            db.Roles.Add(new Role { RoleName = "PATRON" });
            await db.SaveChangesAsync();
            var data = new TestDataFactory(db, clock);
            var patron = await data.CreatePatronAsync($"checkout-{Guid.NewGuid():N}@example.test");
            patronId = patron.Person.Id;
            var performance = await data.CreatePerformanceAsync(1, 42m);
            performanceId = performance.Id;
            seatId = await db.PerformanceSeats.Where(s => s.PerformanceId == performanceId).Select(s => s.SeatId).SingleAsync();
            await new HoldService(db, new PriceResolutionService(), clock)
                .CreateAsync(patronId, performanceId, [seatId], CancellationToken.None);
        }

        var cardNumber = Environment.GetEnvironmentVariable("THEATER_TEST_PAYMENT_CARD");
        if (string.IsNullOrWhiteSpace(cardNumber))
            throw new InvalidOperationException("Set THEATER_TEST_PAYMENT_CARD to a local mock-payment test value before running checkout integration tests.");

        var checkouts = Enumerable.Range(0, 2).Select(async _ =>
        {
            await using var db = postgres.CreateDbContext();
            var paymentConfiguration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MockPayment:AcceptedCardNumber"] = cardNumber
            }).Build();
            var controller = new OrdersController(new OrderService(db, clock, new NullLogSink(), new MockPaymentService(paymentConfiguration)))
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
            var response = await controller.Checkout(new CheckoutRequest("CREDIT_CARD", cardNumber, null), null, CancellationToken.None);
            return response.Result is ObjectResult result ? result.StatusCode ?? 200 : 200;
        });

        var statuses = await Task.WhenAll(checkouts);
        Assert.Equal(1, statuses.Count(status => status == 200));
        Assert.Equal(1, statuses.Count(status => status == 409));
    }

    private sealed class NullLogSink : ILogSink
    {
        public Task LoginAttemptAsync(int? personId, string ipAddress, bool success, CancellationToken ct = default) => Task.CompletedTask;
        public Task ErrorAsync(int? personId, string endpoint, Exception exception, CancellationToken ct = default) => Task.CompletedTask;
        public Task AuditAsync(int staffId, string tableName, int recordId, string action, Dictionary<string, object?>? oldValue = null, Dictionary<string, object?>? newValue = null, CancellationToken ct = default) => Task.CompletedTask;
        public Task PermissionChangeAsync(int personId, int? changedBy, int? oldRole, int newRole, CancellationToken ct = default) => Task.CompletedTask;
        public Task PerformanceStatusAsync(int performanceId, int? changedBy, string? previous, string current, CancellationToken ct = default) => Task.CompletedTask;
    }
}
