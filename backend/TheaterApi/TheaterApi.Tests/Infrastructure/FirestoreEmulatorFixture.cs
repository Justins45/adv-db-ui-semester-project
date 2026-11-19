using Google.Cloud.Firestore;

namespace TheaterApi.Tests.Infrastructure;

public sealed class FirestoreEmulatorFixture : IAsyncLifetime
{
    private readonly string _projectId = $"theater-tests-{Guid.NewGuid():N}";
    public FirestoreDb Database { get; private set; } = null!;

    public Task InitializeAsync()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("FIRESTORE_EMULATOR_HOST")))
            throw new InvalidOperationException(
                "Firestore integration tests require FIRESTORE_EMULATOR_HOST; start the Firestore emulator and set the variable.");

        Database = FirestoreDb.Create(_projectId);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (Database is null) return;
        foreach (var collection in new[] { "audit_log", "permission_change_log", "performance_status_log", "error_log", "login_history", "logs" })
        {
            var snapshot = await Database.Collection(collection).GetSnapshotAsync();
            if (snapshot.Count == 0) continue;
            var batch = Database.StartBatch();
            foreach (var document in snapshot.Documents) batch.Delete(document.Reference);
            await batch.CommitAsync();
        }
    }
}
