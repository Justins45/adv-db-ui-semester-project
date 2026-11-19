using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.Swagger;
using TheaterApi.Tests.Infrastructure;

namespace TheaterApi.Tests.Infrastructure;

public sealed class ApiRouteTests
{
    [Fact]
    public void Row_4_1_AllControllerRoutesHaveApiPrefix()
    {
        var outsideApi = typeof(Program).Assembly.GetTypes()
            .Where(type => !type.IsAbstract && typeof(ControllerBase).IsAssignableFrom(type))
            .SelectMany(type => type.GetCustomAttributes(typeof(RouteAttribute), inherit: true)
                .Cast<RouteAttribute>()
                .Select(attribute => (Controller: type.Name, Route: attribute.Template)))
            .Where(item => !item.Route.TrimStart('/').StartsWith("api", StringComparison.OrdinalIgnoreCase))
            .Select(item => $"{item.Controller}:{item.Route}")
            .ToArray();

        Assert.Empty(outsideApi);
    }
}

public sealed class CorsPolicyTests(TestPostgresFixture postgres) : IClassFixture<TestPostgresFixture>
{
    [Fact]
    public async Task Row_4_13_DisallowedOriginGetsNoCorsHeaders()
    {
        await using var factory = new TheaterApiFactory(postgres, new Microsoft.Extensions.Time.Testing.FakeTimeProvider());
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/events");
        request.Headers.Add("Origin", "https://not-allowed.example");
        request.Headers.Add("Access-Control-Request-Method", "GET");

        using var response = await client.SendAsync(request);

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }
}

public sealed class SwaggerSummaryTests(TestPostgresFixture postgres) : IClassFixture<TestPostgresFixture>
{
    [Fact]
    public void Row_0_6_EverySwaggerOperationHasSummary()
    {
        using var factory = new TheaterApiFactory(postgres, new Microsoft.Extensions.Time.Testing.FakeTimeProvider());
        var swagger = factory.Services.GetRequiredService<ISwaggerProvider>().GetSwagger("v1");

        var missing = swagger.Paths
            .SelectMany(path => path.Value.Operations
                .Where(operation => string.IsNullOrWhiteSpace(operation.Value.Summary))
                .Select(operation => $"{operation.Key} {path.Key}"))
            .ToArray();

        Assert.Empty(missing);
    }
}
