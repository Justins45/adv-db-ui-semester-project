using Microsoft.AspNetCore.Mvc;

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
