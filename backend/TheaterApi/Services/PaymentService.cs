namespace TheaterApi.Services;

public sealed record MockPaymentResult(bool Approved, string? Reference);

public interface IPaymentService
{
    Task<MockPaymentResult> ChargeAsync(string? cardNumber, CancellationToken ct);
}

public sealed class MockPaymentService(IConfiguration configuration) : IPaymentService
{
    private readonly string _acceptedValue = configuration["MockPayment:AcceptedCardNumber"] ?? "";

    public Task<MockPaymentResult> ChargeAsync(string? cardNumber, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var supplied = Normalize(cardNumber);
        var accepted = Normalize(_acceptedValue);
        var approved = accepted.Length > 0 && supplied == accepted;
        return Task.FromResult(new MockPaymentResult(
            approved,
            approved ? $"MOCK-{Guid.NewGuid()}" : null));
    }

    private static string Normalize(string? value) =>
        value?.Replace(" ", "", StringComparison.Ordinal).Replace("-", "", StringComparison.Ordinal) ?? "";
}
