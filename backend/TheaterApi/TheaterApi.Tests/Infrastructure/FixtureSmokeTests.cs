using Google.Cloud.Firestore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using TheaterApi.Tests.Infrastructure;

namespace TheaterApi.Tests;

public sealed class PostgresFixtureSmokeTests(TestPostgresFixture postgres) : IClassFixture<TestPostgresFixture>
{
    [Fact]
    public async Task PostgresFixture_AppliesMigrationsAndConnects()
    {
        await using var db = postgres.CreateDbContext();
        Assert.True(await db.Database.CanConnectAsync());
        Assert.NotEmpty(db.Database.GetAppliedMigrations());
    }
}

public sealed class FirestoreFixtureSmokeTests : IClassFixture<FirestoreEmulatorFixture>
{
    private readonly FirestoreEmulatorFixture _fixture;
    public FirestoreFixtureSmokeTests(FirestoreEmulatorFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task FirestoreFixture_ConnectsToEmulator()
    {
        var snapshot = await _fixture.Database.Collection("fixture_smoke").Limit(1).GetSnapshotAsync();
        Assert.NotNull(snapshot);
    }
}

public sealed class FakeClockSmokeTests
{
    [Fact]
    public void FakeTimeProvider_AdvancesDeterministically()
    {
        var start = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        var clock = new FakeTimeProvider(start);
        clock.Advance(TimeSpan.FromMinutes(10));
        Assert.Equal(start.AddMinutes(10), clock.GetUtcNow());
    }
}
