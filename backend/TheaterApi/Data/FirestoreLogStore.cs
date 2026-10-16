using Google.Cloud.Firestore;
using TheaterApi.Models;

namespace TheaterApi.Data;

public class FirestoreLogStore(FirestoreDb db) : ILogStore
{
    private const string Collection = "logs";

    public Task WriteAsync(LogEntry entry) =>
        db.Collection(Collection).AddAsync(entry);

    public async Task<IReadOnlyList<LogEntry>> GetRecentAsync(int count = 20)
    {
        var snapshot = await db.Collection(Collection)
            .OrderByDescending("CreatedAt")
            .Limit(count)
            .GetSnapshotAsync();

        return snapshot.Documents.Select(d => d.ConvertTo<LogEntry>()).ToList();
    }
}