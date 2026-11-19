using System.Threading.Channels;
using TheaterApi.Models.Logs;

namespace TheaterApi.Logging;

public interface ILogSink
{
    Task LoginAttemptAsync(int? personId, string ipAddress, bool success, CancellationToken ct = default);
    Task ErrorAsync(int? personId, string endpoint, Exception exception, CancellationToken ct = default);
    Task AuditAsync(int staffId, string tableName, int recordId, string action, Dictionary<string, object?>? oldValue = null, Dictionary<string, object?>? newValue = null, CancellationToken ct = default);
    Task PermissionChangeAsync(int personId, int? changedBy, int? oldRole, int newRole, CancellationToken ct = default);
    Task PerformanceStatusAsync(int performanceId, int? changedBy, string? previous, string current, CancellationToken ct = default);
}

public sealed class ChannelLogSink(Channel<LogWorkItem> channel, TimeProvider clock) : ILogSink
{
    private readonly ChannelWriter<LogWorkItem> writer = channel.Writer;
    private string Now() => clock.GetUtcNow().ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
    public Task LoginAttemptAsync(int? personId, string ipAddress, bool success, CancellationToken ct = default) => Enqueue(new LoginHistory { PersonId = personId ?? 0, IpAddress = ipAddress, Success = success, Timestamp = Now() });
    public Task ErrorAsync(int? personId, string endpoint, Exception exception, CancellationToken ct = default) => Enqueue(new ErrorLog { PersonId = personId, EndPoint = endpoint, ErrorMessage = exception.Message, StackTrace = exception.StackTrace, Timestamp = Now() });
    public Task AuditAsync(int staffId, string tableName, int recordId, string action, Dictionary<string, object?>? oldValue = null, Dictionary<string, object?>? newValue = null, CancellationToken ct = default) => Enqueue(new AuditLog { StaffId = staffId, TableName = tableName, RecordId = recordId, Action = action, ChangedAt = Now(), Changes = new AuditChanges { Old = oldValue, New = newValue }, Context = new AuditContext { Source = "api" } });
    public Task PermissionChangeAsync(int personId, int? changedBy, int? oldRole, int newRole, CancellationToken ct = default) => Enqueue(new PermissionChangeLog { PersonId = personId, ChangedBy = changedBy, OldRole = oldRole, NewRole = newRole, DateTime = Now() });
    public Task PerformanceStatusAsync(int performanceId, int? changedBy, string? previous, string current, CancellationToken ct = default) => Enqueue(new PerformanceStatusLog { PerformanceId = performanceId, ChangedBy = changedBy, PreviousStatus = previous, NewStatus = current, DateTime = Now() });
    private Task Enqueue(ILogDocument document) { writer.TryWrite(new LogWorkItem(document)); return Task.CompletedTask; }
}

public sealed record LogWorkItem(ILogDocument Document);
