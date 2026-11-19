using TheaterApi.Services;
using Microsoft.Extensions.Configuration;

namespace TheaterApi.Tests.Orders;

public sealed class PaymentAbstractionTests
{
    [Fact]
    public void Row_6_4_OrderServiceDependsOnPaymentInterface()
    {
        var contract = typeof(OrderService).Assembly.GetType("TheaterApi.Services.IPaymentService");
        Assert.NotNull(contract);
        var constructor = typeof(OrderService).GetConstructors().Single();
        Assert.Contains(constructor.GetParameters(), parameter => parameter.ParameterType == contract);
    }

    [Fact]
    public async Task MockPaymentService_IgnoresSpacesAndDashesAndReturnsMockReference()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["MockPayment:AcceptedCardNumber"] = "approved test value"
        }).Build();
        var service = new MockPaymentService(configuration);

        var result = await service.ChargeAsync("approved-test-value", CancellationToken.None);

        Assert.True(result.Approved);
        Assert.StartsWith("MOCK-", result.Reference);
        Assert.True(Guid.TryParse(result.Reference![5..], out _));
    }

    [Fact]
    public async Task MockPaymentService_DeclinesAnyOtherValueWithoutEchoingIt()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["MockPayment:AcceptedCardNumber"] = "approved test value"
        }).Build();
        var service = new MockPaymentService(configuration);

        var result = await service.ChargeAsync("declined-value", CancellationToken.None);

        Assert.False(result.Approved);
        Assert.Null(result.Reference);
    }
}
