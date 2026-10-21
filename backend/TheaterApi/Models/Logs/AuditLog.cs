using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Google.Cloud.Firestore;
using TheaterApi.Data;

namespace TheaterApi.Models.Logs;

[FirestoreData]
public class AuditLog : ILogDocument
{
    [FirestoreProperty("id")] [JsonPropertyName("id")]
    public int Id { get; set; }

    [FirestoreProperty("staff_id")] [JsonPropertyName("staff_id")]
    public int StaffId { get; set; }

    [FirestoreProperty("table_name")] [JsonPropertyName("table_name")]
    public string TableName { get; set; } = "";

    [FirestoreProperty("record_id")] [JsonPropertyName("record_id")]
    public int RecordId { get; set; }

    [FirestoreProperty("action")] [JsonPropertyName("action")]
    public string Action { get; set; } = "";

    [FirestoreProperty("changed_at", ConverterType = typeof(FlexibleDateTimeConverter))]
    [JsonPropertyName("changed_at")]
    public DateTime ChangedAt { get; set; }

    [FirestoreProperty("changes")] [JsonPropertyName("changes")]
    public AuditChanges Changes { get; set; } = new();

    [FirestoreProperty("context")] [JsonPropertyName("context")]
    public AuditContext Context { get; set; } = new();

    [FirestoreProperty("tags")] [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = [];
}

/// <summary>INSERT rows only have "new", DELETE rows only have "old", UPDATE rows have both.</summary>
[FirestoreData]
public class AuditChanges
{
    [FirestoreProperty("old")] [JsonPropertyName("old")]
    public Dictionary<string, object?>? Old { get; set; }

    [FirestoreProperty("new")] [JsonPropertyName("new")]
    public Dictionary<string, object?>? New { get; set; }
}

[FirestoreData]
public class AuditContext
{
    [FirestoreProperty("ip_address")] [JsonPropertyName("ip_address")]
    public string? IpAddress { get; set; }

    [FirestoreProperty("source")] [JsonPropertyName("source")]
    public string Source { get; set; } = "";
}

/// <summary>Body for POST and PUT. No id or timestamp: the server owns those.</summary>
public class AuditLogRequest
{
    [JsonPropertyName("staff_id")]
    public int StaffId { get; set; }

    [Required] [JsonPropertyName("table_name")]
    public string TableName { get; set; } = "";

    [JsonPropertyName("record_id")]
    public int RecordId { get; set; }

    [Required] [AllowedValues("INSERT", "UPDATE", "DELETE")] [JsonPropertyName("action")]
    public string Action { get; set; } = "";

    [JsonPropertyName("changes")]
    public AuditChanges Changes { get; set; } = new();

    [JsonPropertyName("context")]
    public AuditContext Context { get; set; } = new();

    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = [];

    /// <summary>New document: stamps the time.</summary>
    public AuditLog ToEntity()
    {
        var log = new AuditLog { ChangedAt = DateTime.UtcNow };
        ApplyTo(log);
        return log;
    }

    /// <summary>Copies editable fields onto an existing document (keeps Id and ChangedAt).</summary>
    public void ApplyTo(AuditLog log)
    {
        log.StaffId = StaffId;
        log.TableName = TableName;
        log.RecordId = RecordId;
        log.Action = Action;
        log.Changes = new AuditChanges
        {
            Old = JsonValues.NormalizeMap(Changes.Old),
            New = JsonValues.NormalizeMap(Changes.New)
        };
        log.Context = new AuditContext { IpAddress = Context.IpAddress, Source = Context.Source };
        log.Tags = [.. Tags];
    }
}