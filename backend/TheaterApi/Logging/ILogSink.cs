namespace TheaterApi.Logging;

public interface ILogSink
{
    Task LoginAttemptAsync(int? personId, string ipAddress, bool success, CancellationToken ct = default);
}

public sealed class InMemoryLogSink : ILogSink
{
    private readonly List<LoginAttempt> _attempts = [];
    private readonly Lock _lock = new();
    public IReadOnlyList<LoginAttempt> Attempts { get { lock (_lock) return _attempts.ToArray(); } }
    public Task LoginAttemptAsync(int? personId, string ipAddress, bool success, CancellationToken ct = default)
    { lock (_lock) _attempts.Add(new(personId, ipAddress, success, DateTimeOffset.UtcNow)); return Task.CompletedTask; }
}
public sealed record LoginAttempt(int? PersonId, string IpAddress, bool Success, DateTimeOffset Timestamp);
