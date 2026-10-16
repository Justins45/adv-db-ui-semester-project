using Google.Cloud.Firestore;

namespace TheaterApi.Models;

[FirestoreData]
public class LogEntry
{
    [FirestoreProperty] public string Level { get; set; } = "Info";
    [FirestoreProperty] public string Message { get; set; } = "";
    [FirestoreProperty] public string? UserId { get; set; }
    [FirestoreProperty] public Timestamp CreatedAt { get; set; } = Timestamp.GetCurrentTimestamp();
}