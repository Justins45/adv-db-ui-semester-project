using System.Text.Json.Serialization;
using Google.Cloud.Firestore;

namespace TheaterApi.Models.Logs;

[FirestoreData]
public class LoginHistory : ILogDocument
{
    [FirestoreProperty("id")] [JsonPropertyName("id")] public int Id { get; set; }
    [FirestoreProperty("person_id")] [JsonPropertyName("person_id")] public int PersonId { get; set; }
    [FirestoreProperty("ip_address")] [JsonPropertyName("ip_address")] public string IpAddress { get; set; } = "";
    [FirestoreProperty("success")] [JsonPropertyName("success")] public bool Success { get; set; }
    [FirestoreProperty("timestamp")] [JsonPropertyName("timestamp")] public string Timestamp { get; set; } = "";
}

public class LoginHistoryRequest
{
    [JsonPropertyName("person_id")] public int PersonId { get; set; }
    [JsonPropertyName("ip_address")] public string IpAddress { get; set; } = "";
    [JsonPropertyName("success")] public bool Success { get; set; }
    public LoginHistory ToEntity(TimeProvider clock) => new() { PersonId = PersonId, IpAddress = IpAddress, Success = Success, Timestamp = clock.GetUtcNow().ToString("yyyy-MM-ddTHH:mm:ssZ") };
    public void ApplyTo(LoginHistory log) { log.PersonId = PersonId; log.IpAddress = IpAddress; log.Success = Success; }
}
