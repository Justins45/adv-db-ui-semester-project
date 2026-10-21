using Google.Cloud.Firestore;
using TheaterApi.Models;
using TheaterApi.Models.Logs;

namespace TheaterApi.Service;

// Services/AuditLogService.cs
public class AuditLogService(FirestoreDb firestore)
{
    public async Task<AuditLogCounts> GetCountsAsync()
    {
        var insert = CountByActionAsync("INSERT");
        var update = CountByActionAsync("UPDATE");
        var delete = CountByActionAsync("DELETE");

        await Task.WhenAll(insert, update, delete);

        return new AuditLogCounts(insert.Result, update.Result, delete.Result);
    }

    private async Task<long> CountByActionAsync(string action)
    {
        var result = await firestore.Collection("audit_log")
            .WhereEqualTo("action", action)
            .Count()
            .GetSnapshotAsync();

        return result.Count ?? 0;
    }

    public async Task<List<AuditLog>> GetRecentAsync(int daysAgo)
    {
        var cutoff = Timestamp.FromDateTime(DateTime.UtcNow.AddDays(-daysAgo));

        var snapshot = await firestore.Collection("audit_log")
            .WhereGreaterThanOrEqualTo("changed_at", cutoff)
            .OrderByDescending("changed_at")
            .GetSnapshotAsync();

        return snapshot.Documents
            .Select(d => d.ConvertTo<AuditLog>())
            .ToList();
    }
}