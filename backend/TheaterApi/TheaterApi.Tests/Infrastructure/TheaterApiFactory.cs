using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using System.Security.Cryptography;
using TheaterApi.Data;

namespace TheaterApi.Tests.Infrastructure;

public sealed class TheaterApiFactory(TestPostgresFixture postgres, FakeTimeProvider clock) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:NeonDb"] = postgres.ConnectionString,
            ["Jwt:SigningKey"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
            ["Jwt:Issuer"] = "TheaterApi.Tests",
            ["Jwt:Audience"] = "TheaterApi.Tests",
            ["Firestore:ProjectId"] = "theater-api-tests",
            ["MockPayment:AcceptedCardNumber"] = Environment.GetEnvironmentVariable("THEATER_TEST_PAYMENT_CARD")
        }));
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(clock);
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options => options.UseNpgsql(postgres.ConnectionString));
        });
    }
}
