using System.Threading.Channels;
using TheaterApi.Data;
using TheaterApi.Logging;
using TheaterApi.Models.Logs;

namespace TheaterApi.BackgroundJobs;

public sealed class FirestoreLogWriter(Channel<LogWorkItem> channel, IServiceScopeFactory scopes, ILogger<FirestoreLogWriter> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var item in channel.Reader.ReadAllAsync(stoppingToken))
        {
            try { using var scope = scopes.CreateScope(); await WriteAsync(scope.ServiceProvider, item.Document, stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
            catch (Exception ex) { logger.LogError(ex, "Unable to write Firestore log."); }
        }
    }

    private static Task WriteAsync(IServiceProvider services, ILogDocument document, CancellationToken ct) => document switch
    {
        AuditLog x => services.GetRequiredService<ILogRepository<AuditLog>>().CreateAsync(x, ct),
        PermissionChangeLog x => services.GetRequiredService<ILogRepository<PermissionChangeLog>>().CreateAsync(x, ct),
        PerformanceStatusLog x => services.GetRequiredService<ILogRepository<PerformanceStatusLog>>().CreateAsync(x, ct),
        ErrorLog x => services.GetRequiredService<ILogRepository<ErrorLog>>().CreateAsync(x, ct),
        LoginHistory x => services.GetRequiredService<ILogRepository<LoginHistory>>().CreateAsync(x, ct),
        _ => Task.CompletedTask
    };
}
