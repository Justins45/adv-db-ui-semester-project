using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using TheaterApi.Data;

namespace TheaterApi.Tests.Infrastructure;

public sealed class TestPostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("pgvector/pgvector:pg16")
        .WithDatabase("theater_tests")
        .WithUsername("theater_tests")
        .WithPassword("test-only-password")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        await using var db = CreateDbContext();
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();

    public AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        return new AppDbContext(options);
    }

    public async Task ResetAsync()
    {
        await using var db = CreateDbContext();
        await db.Database.ExecuteSqlRawAsync("""
            TRUNCATE TABLE "ORDER", order_item, payment, ticket, cart_item, cart, performance_seat,
                price_tier, performance_cast, performance, seat, section, stage, venue, event_genre,
                genre, promo_code, email_list, password_reset_request, performer, staff, patron, person,
                role, review RESTART IDENTITY CASCADE
            """);
    }
}
