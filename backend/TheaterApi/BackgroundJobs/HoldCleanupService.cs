using TheaterApi.Services;

namespace TheaterApi.BackgroundJobs;
public sealed class HoldCleanupService(IServiceScopeFactory scopes, ILogger<HoldCleanupService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
        while (await timer.WaitForNextTickAsync(stoppingToken))
            try { using var scope = scopes.CreateScope(); await scope.ServiceProvider.GetRequiredService<HoldService>().CleanupExpiredAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
            catch (Exception e) { logger.LogError(e, "Expired hold cleanup failed."); }
    }
}
