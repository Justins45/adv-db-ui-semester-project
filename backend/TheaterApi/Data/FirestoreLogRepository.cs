using Google.Cloud.Firestore;
using TheaterApi.Models.Logs;

namespace TheaterApi.Data;

public class FirestoreLogRepository<T>(FirestoreDb db, string collectionName) : ILogRepository<T>
    where T : class, ILogDocument
{
    private CollectionReference Collection => db.Collection(collectionName);

    // READ (list): newest first. Ordering by "id" uses Firestore's automatic
    // single-field index, so no index setup is needed.
    public async Task<IReadOnlyList<T>> GetRecentAsync(int limit, CancellationToken ct = default)
    {
        var snapshot = await Collection
            .OrderByDescending("id")
            .Limit(limit)
            .GetSnapshotAsync(ct);

        return snapshot.Documents.Select(d => d.ConvertTo<T>()).ToList();
    }

    // READ (one)
    public async Task<T?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var snapshot = await Collection.Document(id.ToString()).GetSnapshotAsync(ct);
        return snapshot.Exists ? snapshot.ConvertTo<T>() : null;
    }

    // CREATE: Firestore has no auto-increment, so find the highest id and add 1.
    // Doing the read and the write in one transaction stops two simultaneous
    // requests from claiming the same id (Firestore retries the loser).
    public Task<T> CreateAsync(T entity, CancellationToken ct = default) =>
        db.RunTransactionAsync(async tx =>
        {
            var latest = await tx.GetSnapshotAsync(
                Collection.OrderByDescending("id").Limit(1), ct);

            var lastId = latest.Documents.FirstOrDefault()?.GetValue<int>("id") ?? 0;
            entity.Id = lastId + 1;

            // Create() fails if the document already exists, so it never overwrites.
            tx.Create(Collection.Document(entity.Id.ToString()), entity);
            return entity;
        }, cancellationToken: ct);

    // UPDATE: read, let the caller modify, write back, all atomically.
    // Returns null when the document doesn't exist (the controller turns that into a 404).
    public Task<T?> UpdateAsync(int id, Action<T> apply, CancellationToken ct = default) =>
        db.RunTransactionAsync<T?>(async tx =>
        {
            var docRef = Collection.Document(id.ToString());
            var snapshot = await tx.GetSnapshotAsync(docRef, ct);
            if (!snapshot.Exists) return null;

            var entity = snapshot.ConvertTo<T>();
            apply(entity);
            entity.Id = id;
            tx.Set(docRef, entity);
            return entity;
        }, cancellationToken: ct);

    // DELETE
    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var docRef = Collection.Document(id.ToString());
        if (!(await docRef.GetSnapshotAsync(ct)).Exists) return false;

        await docRef.DeleteAsync(cancellationToken: ct);
        return true;
    }
}