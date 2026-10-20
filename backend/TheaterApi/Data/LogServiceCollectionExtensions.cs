using Google.Cloud.Firestore;
using TheaterApi.Models.Logs;

namespace TheaterApi.Data;

public static class LogServiceCollectionExtensions
{
    public static IServiceCollection AddFirestoreLogs(this IServiceCollection services)
    {
        AddLog<AuditLog>(services, "audit_log");
        AddLog<PermissionChangeLog>(services, "permission_change_log");
        AddLog<PerformanceStatusLog>(services, "performance_status_log");
        AddLog<ErrorLog>(services, "error_log");
        return services;
    }

    private static void AddLog<T>(IServiceCollection services, string collection)
        where T : class, ILogDocument =>
        services.AddSingleton<ILogRepository<T>>(sp =>
            new FirestoreLogRepository<T>(sp.GetRequiredService<FirestoreDb>(), collection));
}