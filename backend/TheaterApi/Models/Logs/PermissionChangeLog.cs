using System.Text.Json.Serialization;
using Google.Cloud.Firestore;
using TheaterApi.Data;

namespace TheaterApi.Models.Logs;

[FirestoreData]
public class PermissionChangeLog : ILogDocument
{
    [FirestoreProperty("id")] [JsonPropertyName("id")]
    public int Id { get; set; }

    [FirestoreProperty("person_id")] [JsonPropertyName("person_id")]
    public int PersonId { get; set; }

    [FirestoreProperty("changed_by")] [JsonPropertyName("changed_by")]
    public int? ChangedBy { get; set; }

    [FirestoreProperty("old_role")] [JsonPropertyName("old_role")]
    public int? OldRole { get; set; }

    [FirestoreProperty("new_role")] [JsonPropertyName("new_role")]
    public int NewRole { get; set; }

    [FirestoreProperty("date_time", ConverterType = typeof(FlexibleDateTimeConverter))]
    [JsonPropertyName("date_time")]
    public DateTime DateTime { get; set; }
}

public class PermissionChangeLogRequest
{
    [JsonPropertyName("person_id")]
    public int PersonId { get; set; }

    [JsonPropertyName("changed_by")]
    public int? ChangedBy { get; set; }

    [JsonPropertyName("old_role")]
    public int? OldRole { get; set; }

    [JsonPropertyName("new_role")]
    public int NewRole { get; set; }

    public PermissionChangeLog ToEntity()
    {
        var log = new PermissionChangeLog { DateTime = System.DateTime.UtcNow };
        ApplyTo(log);
        return log;
    }

    public void ApplyTo(PermissionChangeLog log)
    {
        log.PersonId = PersonId;
        log.ChangedBy = ChangedBy;
        log.OldRole = OldRole;
        log.NewRole = NewRole;
    }
}